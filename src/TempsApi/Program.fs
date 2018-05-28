module TempsApi.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Http
open EventStore.ClientAPI
open System.Net
open System.Text

type ILogger = Microsoft.Extensions.Logging.ILogger

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open GiraffeViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "TempsApi" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "TempsApi" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------
[<CLIMutable>]
type TempReading = 
    {
        Timestamp : DateTime
        TempF : decimal
        SensorId : string
    }

type ReadingMetadata = 
    {
        TimeReceived : DateTime
    }

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view

let writeEvent (tempReading : TempReading, esConnection : IEventStoreConnection) =
    let data = JsonUtil.serialize tempReading |> Encoding.UTF8.GetBytes
    let metadata = { TimeReceived = DateTime.UtcNow } |> JsonUtil.serialize |> Encoding.UTF8.GetBytes

    let event = new EventData(Guid.NewGuid(), "temperature-reading", true, data, metadata)
    esConnection.AppendToStreamAsync("temperature-readings", (int64)ExpectedVersion.Any, event).Wait()


let postReading = 
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let logger = ctx.GetLogger()
        task {
            let! reading = ctx.BindJsonAsync<TempReading>()

            logger.LogInformation(sprintf "Read temp value of %M" reading.TempF)
            let result = writeEvent(reading, ctx.GetService<IEventStoreConnection>())
            // return
            return! Successful.OK reading next ctx
        }

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
            ]
        POST >=> route "/" >=> postReading
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let conn = EventStoreConnection.Create(new IPEndPoint(IPAddress.Loopback, 1113))
    conn.ConnectAsync().Wait()
    services.AddSingleton(conn) |> ignore
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = (int)l >= (int)LogLevel.Information
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0