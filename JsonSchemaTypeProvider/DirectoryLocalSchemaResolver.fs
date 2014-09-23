namespace Jet.Schemas.Resolvers

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection

open System.Diagnostics

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema

type DirectoryLocalSchemaResolver (basePath:string) =
    inherit JsonSchemaResolver()

    do Trace.WriteLine (sprintf "DirectoryLocalSchemaResolver: %s" basePath)

    let (|MatchInternalReference|_|) (str:string) =
        if str.StartsWith("#") then Some str else None

    let (|MatchFileReference|_|) (str:string) =
        if str.StartsWith("file:") then Some str else None

    let (|MatchHttpReference|_|) (str:string) =
        if str.StartsWith("http:") then Some str else None

    member this.BasePath = basePath

    override this.GetSchema (reference:string) = 
        Trace.WriteLine (sprintf "ResolveReference: %s" reference)
        match reference with
        | MatchInternalReference r -> 
            Trace.WriteLine (sprintf "ResolveReference-INTERNAL: %s" reference)
            base.GetSchema reference
        | MatchFileReference r | MatchHttpReference r ->
            Trace.WriteLine (sprintf "ResolveReference-REF: %s" reference)
            let parseOk, uri = Uri.TryCreate(reference, UriKind.RelativeOrAbsolute)
            if parseOk then
                Trace.WriteLine (sprintf "ResolveReference-URI: %A" uri)
                let mutable schemaContents = null:string
                if uri.IsFile then
                    let loadPath = Path.Combine(basePath, uri.LocalPath.Substring(2))
                    Trace.WriteLine (sprintf "ResolveReference-FILEPATH: %s" loadPath)
                    schemaContents <- System.IO.File.ReadAllText(loadPath)
                else
                    let req = new System.Net.WebClient()
                    schemaContents <- req.DownloadString uri

                let referencedSchema = JsonSchema.Parse(schemaContents, this)
                Trace.WriteLine (sprintf "ResolveReference-PARSED: %s" referencedSchema.Id)
                Trace.WriteLine (sprintf "ResolveReference-CONTENTS: %s" (referencedSchema.ToString()))
                referencedSchema
            else
                base.GetSchema reference
        | _ -> base.GetSchema reference

