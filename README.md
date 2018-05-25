# TempsApi

A [Giraffe](https://github.com/giraffe-fsharp/Giraffe) web application, which has been created via the `dotnet new giraffe` command.

## Build and test the application

### Windows

Start EventStore:
```
EventStore.ClusterNode.exe --db ~/source/repos/eventstore/db --log ~/source/repos/eventstore/logs
```
Check out the admin page at http://127.0.0.1:2113

Run the `build.bat` script in order to restore, build and test (if you've selected to include tests) the application:

```
> ./build.bat
```

### Linux/macOS

Run the `build.sh` script in order to restore, build and test (if you've selected to include tests) the application:

```
$ ./build.sh
```

## Run the application

After a successful build you can start the web application by executing the following command in your terminal:

```
dotnet run src/TempsApi
```

After the application has started visit [http://localhost:5000](http://localhost:5000) in your preferred browser.