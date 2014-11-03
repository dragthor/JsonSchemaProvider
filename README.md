JsonSchemaProvider
==================

This is generative type provider, meaning that it creates true .net types equally accessible from any .Net language. It makes use of excellent Json.Net 
library to do actual schema parsing and validation.

Typical usage
-------------

    type Schema = JsonSchemaProvider<"schemas">

    let z = Schema.ZipLookup(status = "success")
    z.message <-  "foo"

Type provider searches for any files matching `*.schema.json` in folder `schemas` and generate POCOs which can be than instantiated as normal .net types.
These types expose:

- `Deserialize` static method for parsing 
- `Serialize` instance method returning validated Json.Net `JObject`
- `GetSchema` static method returning raw schema (the reason it returns a string and not `JsonSchema` object is that presently Visual Studio includes version 4.5 of Json.Net library, which breaks Type Provider 
as it relies on version 6.0.0 of Json.Net for parsing):
    
    let jsonString = z.Serialize()
    let z = Schema.ZipLookup.Deserialize jsonString
    let schema = Schema.ZipLookup.GetSchema()    


Restrictions and limitations
--------

- Every `object` definition is expected to have type property set up as well, it is used as POCO type name
- It is strongly recommended to include `"additionalProperties" : false` with each object definition - otherwise pretty much any json will pass validation

TODO
----

- Json Schema support in Json.Net is experimental and limited to draft 3, it would be great to replace it with custom schema validation
- Type provider might be more useful as erasing as opposed to generative, JsonValue discriminated union from FSharp.Data library sounds like excelent base type
