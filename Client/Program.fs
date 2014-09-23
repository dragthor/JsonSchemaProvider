open Jet.Schemas
open System.Collections.Generic
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema

open TestClient

[<EntryPoint>]
let main argv = 
    let str = System.IO.File.ReadAllText("example.json")
    let json = JObject.Parse(str)
    let acknowledge = Schema.OrderAcknowledge.Deserialize(str)
    printfn "%O" acknowledge.items.[0].id
    printfn "%O" acknowledge.newId
    //printfn "%b" <| (acknowledge.Serialize().ToString() = json.ToString())    

    let json = JObject.Parse(System.IO.File.ReadAllText("bad-example.json"))
    let errors = Validation.GetErrors(Schema.OrderAcknowledge.GetSchema(), json)
    //let errors = json.Validate(schema)
    printfn "%A" errors
    let order = Schema.Item(id = "123", newId = "345")
    printfn "%A" order
    0
