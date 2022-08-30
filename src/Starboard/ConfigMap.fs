namespace Starboard.Resources

open Starboard.Resources
open System.Collections

// https://v1-24.docs.kubernetes.io/docs/reference/generated/kubernetes-api/v1.24/#configmap-v1-core

type ConfigMap = { 
    metadata: Metadata
    data: Map<string,string>
    binaryData: Map<string,string>
    immutable: bool
}

type ConfigMap with
    static member empty =
        { 
            metadata = Metadata.empty
            data = Map.empty
            binaryData = Map.empty
            immutable = false
        }

// resource: version, kind, metadata, spec
// template: metadata, spec
    member this.K8sVersion() = "v1"
    member this.K8sKind() = "ConfigMap"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
    //member this.Spec() =
    //    let mapToMutDict map = map :> System.Collections.Generic.IDictionary<_,_> //|> System.Collections.Generic.Dictionary
    //    {|
    //        data = mapToMutDict this.data
    //        binaryData = mapToMutDict this.binaryData
    //        immutable = this.immutable
    //    |}
    member this.ToResource() =
        let mapToMutDict map = map :> System.Collections.Generic.IDictionary<_,_>
        let emptyAsNone ss =
            if Seq.isEmpty ss then None
            else Some ss
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            data = this.data |> emptyAsNone |> Option.map mapToMutDict 
            binaryData = this.binaryData |> emptyAsNone |> Option.map mapToMutDict 
            immutable = this.immutable
        |}
        

type ConfigMapBuilder() =
    
    member _.Yield (_) = ConfigMap.empty
    /// Name of the ConfigMap. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: ConfigMap, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata }

    /// Namespace of the ConfigMap.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: ConfigMap, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
        
    /// Labels for the ConfigMap
    [<CustomOperation "labels">]
    member _.Labels(state: ConfigMap, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }

    /// Annotations for the ConfigMap
    [<CustomOperation "annotations">]
    member _.Annotations(state: ConfigMap, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }

    /// Adds an item to the ConfigMap data
    [<CustomOperation "item">]
    member _.Item(state: ConfigMap, (key, value)) = { state with data = state.data |> Map.add key value }

    /// Sets ConfigMap data
    [<CustomOperation "data">]
    member _.Data(state: ConfigMap, data: (string*string) list) = 
        { state with data = data |> Map.ofList }

    /// Sets ConfigMap binaryData
    [<CustomOperation "binaryData">]
    member _.BinaryData(state: ConfigMap, binaryData: (string*string) list) = 
        { state with binaryData = binaryData |> Map.ofList }

[<AutoOpen>]
module ConfigMapBuilders =
    let configMap = new ConfigMapBuilder()