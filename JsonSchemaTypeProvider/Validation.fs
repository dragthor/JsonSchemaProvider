namespace Jet.Schemas

open System.Collections.Generic
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema

module Validation =
    let GetErrors(schema: JsonSchema, jobject : JObject) =
        let errors = new ResizeArray<_>()
        let collect _ (args :ValidationEventArgs) = 
            errors.Add <| sprintf "%s Path: %s" args.Message args.Path
        let handler = new ValidationEventHandler(collect)
        jobject.Validate(schema, handler)
        errors |> Seq.distinct |> Array.ofSeq

    
