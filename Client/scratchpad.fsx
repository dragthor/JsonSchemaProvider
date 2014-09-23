#r @"..\packages\Newtonsoft.Json.6.0.3\lib\net45\Newtonsoft.Json.dll"
open System.Collections.Generic
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema
//
//let schema = JsonSchema.Parse(System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + @"\schema.json"))
//let json = JObject.Parse(System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + @"\bad-example.json"))

////let l : IList<string> ref = ref null
////json.IsValid(schema, l)
////
////l.Value.[0]
////
//let errors = new ResizeArray<_>()
//
//let collect _ (args :ValidationEventArgs) = 
//    errors.Add(sprintf "Exception: %A Message: %s Path: %s" args.Exception args.Message args.Path)
//
//let handler = new ValidationEventHandler(collect)
//
//json.Validate(schema, handler)
//
//errors |> Seq.iter (printfn "%s")

//
//#r @"..\SampleProvider\bin\Debug\SampleProvider.dll"
//
//type Birch = Sample.Sample<"birch">
//type Elm = Sample.Sample<"elm">
//
//let birch = Birch.birch()
//let elm = Elm.elm()
//elm.Branches <- ([|Elm.elm()|])

let str = System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + @"\example.json")
let json = JObject.Parse(str)

#r @"bin\Debug\JsonSchemaTypeProvider.dll"

open Jet.Schemas

type Schema = JsonSchemaProvider<"schemas">


let acknowledge = Schema.OrderAcknowledge.Deserialize(str)
printfn "%O" acknowledge.items.[0].id
printfn "%O" acknowledge.newId
//printfn "%b" <| (acknowledge.Serialize().ToString() = json.ToString())    

let j = JObject.Parse(System.IO.File.ReadAllText(__SOURCE_DIRECTORY__ + @"\bad-example.json"))
let errors = Validation.GetErrors(Schema.OrderAcknowledge.GetSchema(), j)
//let errors = json.Validate(schema)
printfn "%A" errors
let order = Schema.Item(id = "123", newId = "345")
printfn "%A" order
