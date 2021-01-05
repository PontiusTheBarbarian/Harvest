open System
open FSharp.Collections
open Farmer
open Farmer.Builders
open System.IO
open YamlDotNet.Serialization
open Harvest.FarmerExtensions

type Resource() =
    [<YamlMember(Alias = "name")>]
    member val Name   = "" with get,set
    [<YamlMember(Alias = "resource_type")>]
    member val ResourceType  = "" with get,set
    [<YamlMember(Alias = "runtime_stack")>]
    member val RunTimeStack    = "" with get,set

type Deployment() =
    [<YamlMember(Alias = "name")>]
    member val Name = "" with get,set
    [<YamlMember(Alias = "location")>]
    member val Location = "" with get,set
    [<YamlMember(Alias = "resources")>]
    member val Resources : Resource[] = [| new Resource() |] with get,set

type Root() =
    [<YamlMember(Alias = "deployment")>]
    member val Deployment : Deployment = new Deployment() with get,set

[<EntryPoint>]
let main argv =
    let filePath = if Array.isEmpty argv then "squash.yaml" else argv.[0]
    let outputFilePath = if Array.isEmpty argv then ".\azuredeploy" else argv.[1]
    let yaml =
        let deserializer = new Deserializer()
        let stream = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read))
        let root = deserializer.Deserialize<Root>(stream);
        stream.Close()
        root.Deployment

    let webApplications =
        yaml.Resources
        |> Seq.filter (fun w -> w.ResourceType = "WebApplication")
        |> Seq.map (fun w ->
            let runtimeStack = Runtime.FromString(w.RunTimeStack)
            webApp {
                name w.Name
                runtime_stack runtimeStack
            })
        |> List.ofSeq

    let deployment =
        let deploymentlocation = Location.FromString(yaml.Location)
        arm {
            location deploymentlocation
            add_resources [ for webApplication in webApplications do webApplication; ]
        }
        |> Writer.quickWrite (outputFilePath + "\\" +  yaml.Name)

    0 //Return 0 to represent program successfully executed


