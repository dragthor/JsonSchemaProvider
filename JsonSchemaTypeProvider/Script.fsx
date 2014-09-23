// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r @"..\packages\Newtonsoft.Json.6.0.3\lib\net45\Newtonsoft.Json.dll"

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema

let schema = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + @"\sample.schema.json")
let jsonSchema = JsonSchema.Parse(schema)

let sample = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + @"\sample.json")

let json = JObject.Parse(sample)

let serializer = new JsonSerializer

//jsonSchema.Properties.["items"].Items |> Seq.iter (printfn "Here: %A")