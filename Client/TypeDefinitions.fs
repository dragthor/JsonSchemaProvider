module TestClient
open System.Collections.Generic
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema
open Jet.Schemas

type Schema = JsonSchemaProvider<"schemas">

let f x = printf "%A" x

