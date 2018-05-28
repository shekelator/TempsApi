module JsonUtil

  open Newtonsoft.Json
    
  let serialize obj =
    JsonConvert.SerializeObject obj

  let deserialize<'a> str =
    try
      JsonConvert.DeserializeObject<'a> str
      |> Result.Ok
    with
      // catch all exceptions and convert to Result
      | ex -> Result.Error ex  