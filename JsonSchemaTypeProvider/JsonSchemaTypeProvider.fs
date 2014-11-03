namespace Jet.Schemas

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations

open Samples.FSharp.ProvidedTypes

open Jet.Schemas.Resolvers

[<assembly:TypeProviderAssembly()>]
do()

[<TypeProvider>]
type JsonSchemaTypeProvider(config: TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let mutable watcher = null : IDisposable

    let mutable basePath = null : string

    let namespaceName = "Jet.Schemas"
        
    let schemaFilter = "*.schema.json"

    let thisAssembly = Assembly.GetExecutingAssembly()

    let providerType = ProvidedTypeDefinition(thisAssembly, namespaceName, "JsonSchemaProvider", Some typeof<obj>, HideObjectMethods = true, IsErased = false)

    let addToTempAssembly t = ProvidedAssembly( Path.ChangeExtension(Path.GetTempFileName(), ".dll")).AddTypes [t]

    do
        System.AppDomain.CurrentDomain.add_AssemblyResolve( System.ResolveEventHandler(fun _ e -> 
            let fi = System.IO.FileInfo(config.RuntimeAssembly)
            Assembly.LoadFile(System.IO.Path.Combine(fi.DirectoryName, ( e.Name.Substring(0,e.Name.IndexOf(",")) + ".dll" ) ))))

        addToTempAssembly providerType

        providerType.DefineStaticParameters(
            parameters = [ ProvidedStaticParameter("SchemaFilePath", typeof<string>) ],
            instantiationFunction = fun typeName args -> 
            
                this.GenerateTypes(typeName, config.ResolutionFolder, unbox args.[0])
        )
        this.AddNamespace(namespaceName, [ providerType ])

    do
        providerType.AddXmlDoc """
    <summary>Type generator for JSON Schema (http://json-schema.org/) based on Json.Net library. As of now, it is compatible with draft 3.</summary> 
    <param name='SchemaFilePath'>Path to a folder with json schema files. They are expected to end with '*.schema.json'</param>
    """
    
    interface IDisposable with 
        member this.Dispose() = 
           if watcher <> null
           then try watcher.Dispose() with _ -> ()
        
    member internal this.GenerateTypes(typeName, resolutionFolder, schemaFilePath) = 
        let resolvedPath = 
            if not (Path.IsPathRooted schemaFilePath) 
            then Path.Combine(resolutionFolder, schemaFilePath)        
            else schemaFilePath            
            
        if not(Directory.Exists resolvedPath) then failwithf "Directory %s is not found." resolvedPath

        let w = new FileSystemWatcher(Filter = schemaFilter, Path = resolvedPath)
        w.Changed.Add(fun _ -> this.Invalidate())
        w.Renamed.Add(fun _ -> this.Invalidate())
        w.EnableRaisingEvents <- true 
        watcher <- w
        
        let rootType = ProvidedTypeDefinition(thisAssembly, namespaceName, typeName, Some typeof<obj>, HideObjectMethods = true, IsErased = false)
        addToTempAssembly rootType
        let acc = ResizeArray<ProvidedTypeDefinition>()
        rootType.AddMembersDelayed (fun () -> 
                System.Diagnostics.Trace.WriteLine("-----------AddMembersDelayed-----------")
                let schemaResolver = new DirectoryLocalSchemaResolver(resolvedPath)
                let acc = ResizeArray<ProvidedTypeDefinition>()
                for schemaFile in Directory.GetFiles(resolvedPath, schemaFilter, SearchOption.AllDirectories) do
                    System.Diagnostics.Trace.WriteLine(sprintf "----PARSING SCHEMA FILE %s ---------" schemaFile)
                    let schema = System.IO.File.ReadAllText(schemaFile)                    
                    let jsonSchema = JsonSchema.Parse(schema, schemaResolver)
                    this.CreateTypeFromSchema (jsonSchema, acc) |> ignore
                let toAdd = acc |> Seq.distinctBy (fun t -> t.Name) |> List.ofSeq 
                System.Diagnostics.Trace.WriteLine(sprintf "Adding types %A" toAdd)
                toAdd                 
        )                
        rootType

    member internal this.CreateTypeFromSchema (jsonSchema, generatedTypes) : Type = 
        System.Diagnostics.Trace.WriteLine(sprintf "-----------CreateTypeFromSchema (%s)-----------" jsonSchema.Title)
        let typeName = jsonSchema.Title        
        if typeName = null then failwithf "Title is required for type='object': %A" jsonSchema        
        match generatedTypes |> Seq.tryFind (fun t -> t.Name = typeName) with
        | Some t -> upcast t
        | None ->
            let generatedType = ProvidedTypeDefinition(typeName,  Some typeof<obj>, IsErased = false)
            addToTempAssembly generatedType
            generatedTypes.Add  generatedType
            generatedType.AddMember <| ProvidedConstructor([], InvokeCode = fun args -> <@@ obj () @@>)             
            generatedType.AddMembers <| this.GenerateFields(jsonSchema, generatedTypes) 
            
            System.Diagnostics.Trace.WriteLine (jsonSchema.ToString())
            let schemaQuot = <@@ JsonSchema.Parse(%%Expr.Value(jsonSchema.ToString())) @@>

            let convert = ProvidedMethod("Deserialize", [ProvidedParameter("json", typeof<string>)], generatedType, IsStaticMethod = true)
            convert.InvokeCode <- fun args -> 
                <@@ 
                    let json = %%args.[0]:string
                    let jobject = JObject.Parse json
                    jobject.Validate(%%schemaQuot)
                    jobject.ToObject(generatedType) 
                @@>
            generatedType.AddMember convert

            let serialize = ProvidedMethod("Serialize", [], typeof<JObject>)
            serialize.InvokeCode <- fun args -> 
                <@@ 
                    let jobject = JObject.FromObject(%%Expr.Coerce(args.[0], typeof<obj>))
                    jobject.Validate(%%schemaQuot)
                    jobject
                @@>
            generatedType.AddMember serialize

            let getSchema = ProvidedMethod("GetSchema", [], typeof<JsonSchema>, IsStaticMethod = true, InvokeCode = fun args -> schemaQuot)
            generatedType.AddMember getSchema

            System.Diagnostics.Trace.WriteLine (sprintf "From schema title:%s, extracted %i types: [%A]" jsonSchema.Title generatedTypes.Count generatedTypes)
            upcast generatedType

    member internal this.GenerateFields (jsonSchema, generatedTypes) : MemberInfo list =
        if jsonSchema.Properties = null 
        then []
        else
        [
            for KeyValue(key,value) in jsonSchema.Properties do
                let propertyType = 
                    match this.ResolveType (value, generatedTypes) with
                    | Choice1Of2 t -> t
                    | Choice2Of2 e -> failwithf "Error: Cant' figure out type of %A in schema %A" key jsonSchema
                let field = ProvidedField("_" + key, propertyType)
                yield upcast field
                let property = ProvidedProperty(key, propertyType, [])
                property.GetterCode <- fun args -> Expr.FieldGet(args.[0], field)
                property.SetterCode <- fun args -> Expr.FieldSet(args.[0], field, args.[1])
                yield upcast property
        ]
   
    member internal this.ResolveType (value, generatedTypes) =
        if not <| value.Type.HasValue then 
            sprintf "Invalid schema: JsonSchemaType cannot be null : %A" value |> Choice2Of2 
        else
            match value.Type.Value with
            | JsonSchemaType.String -> Choice1Of2 typeof<string>
            | JsonSchemaType.Integer -> Choice1Of2 typeof<int>
            | JsonSchemaType.Boolean -> Choice1Of2 typeof<bool>
            | JsonSchemaType.Float -> Choice1Of2 typeof<decimal>
            | JsonSchemaType.Any 
            | JsonSchemaType.None 
            | JsonSchemaType.Null -> Choice1Of2 typeof<obj>
            | JsonSchemaType.Object -> this.CreateTypeFromSchema (value, generatedTypes) |> Choice1Of2
            | JsonSchemaType.Array -> 
                if value.Items = null then Choice1Of2 typeof<obj []>
                else
                    if value.Items.Count <> 1 
                    then sprintf "Exactly one type definition per array is expected, recieved: %A" value.Items |> Choice2Of2
                    else
                        let firstType = value.Items.[0]
                        match this.ResolveType (firstType, generatedTypes) with
                        | Choice1Of2 itemType -> itemType.MakeArrayType() |> Choice1Of2
                        | x -> x
            | _ -> failwithf "Unsupported JsonSchemaType value: %A" (value.Type.Value)
//        if value.Required.HasValue && value.Required.Value = true
//        then typ
//        else typedefof<_ option>.MakeGenericType(typ)
                
