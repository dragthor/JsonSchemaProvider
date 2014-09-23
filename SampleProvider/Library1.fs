namespace Sample.Impl

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open Samples.FSharp.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations

[<assembly:TypeProviderAssembly()>]
do()

[<TypeProvider>]
type TreeProvider(config: TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces()

    let namespaceName = "Sample"
    let thisAssembly = Assembly.GetExecutingAssembly()

    let providerType = ProvidedTypeDefinition(thisAssembly, namespaceName, "Sample", Some typeof<obj>, HideObjectMethods = true, IsErased = false)

    let tempAssembly = ProvidedAssembly( Path.ChangeExtension(Path.GetTempFileName(), ".dll"))

    do
        tempAssembly.AddTypes [providerType ]
        providerType.DefineStaticParameters( parameters = [ ProvidedStaticParameter("treeName", typeof<string>)  ],
            instantiationFunction = (fun typeName args ->
                let rootType = ProvidedTypeDefinition(thisAssembly, namespaceName, typeName, Some typeof<obj>, HideObjectMethods = true, IsErased = false)
                let tempAssembly = ProvidedAssembly( Path.ChangeExtension(Path.GetTempFileName(), ".dll"))       
                tempAssembly.AddTypes [ rootType ]         
                rootType.AddMembersDelayed (fun () -> 
                        let tree =  ProvidedTypeDefinition(unbox args.[0],  Some typeof<obj>, IsErased = false)
                        tree.AddMember <| ProvidedConstructor([], InvokeCode = fun _ -> <@@ obj () @@>)
                        let field = ProvidedField("Branches", tree.MakeArrayType())
                        field.SetFieldAttributes(FieldAttributes.Public)
                        tree.AddMember field
                        tempAssembly.AddTypes [tree]
                        [ tree ]
                )
                rootType
            )
        )
        this.AddNamespace(namespaceName, [ providerType ])