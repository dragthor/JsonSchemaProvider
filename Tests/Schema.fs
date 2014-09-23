module Schema 
    
open Xunit
open FsUnit.Xunit
open Newtonsoft.Json.Linq

open Jet.Schemas    
open TestClient

let jsonStr = """
        {
    "newId": "76",
    "items": [
        {
            "id": "8f5ae15b6b414b00a1b9d6ad99166a00",
            "newId": "76-i105"
        }
    ],
    "shipments": [
        {
            "id": "a172ae641c384c57b3a5b58ae4dd3bd6",
            "newId": "76-s2507",
            "items": [
                {
                    "id": "9af824b9e4a24854991870db184bbe32",
                    "newId": "76-s2507-i105"
                }
            ]
        }
    ]
}
    """


let getJson() = JObject.Parse jsonStr

let getActivation() = jsonStr |> Schema.OrderAcknowledge.Deserialize

[<Fact>]
let ``Validation passes``() = Validation.GetErrors(Schema.OrderAcknowledge.GetSchema(), getJson()) |> should equal [||]

type Schema = JsonSchemaProvider<"schemas">
type Schema1 = JsonSchemaProvider<"schemas">

[<Fact>]
let ``Weird schema bug``() = 
    let z = Schema.ZipLookup(status = "success")
    z.message <-  "foo"
    let z1 = Schema1.ZipLookup(status = "success") 
    z1.message <-  "foo"

[<Fact>]
let ``Validation fails`` () =
    let results = Validation.GetErrors(Schema1.ZipLookup.GetSchema(), JObject())
    results.Length |> should equal 1
    let results = Validation.GetErrors(Schema1.ZipLookup.GetSchema(), JObject())
    results.Length |> should equal 1