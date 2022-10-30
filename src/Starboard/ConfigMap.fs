namespace Starboard.Resources

open Starboard
open Starboard.Resources

//====================================
// ConfigMap
// https://kubernetes.io/docs/concepts/configuration/configmap/
// https://v1-24.docs.kubernetes.io/docs/reference/generated/kubernetes-api/v1.24/#configmap-v1-core
//====================================

type ConfigMap = { 
    metadata: Metadata
    data: Map<string,string>
    binaryData: Map<string,(byte array)>
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
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    //member this.Spec() =
    //    let mapToMutDict map = map :> System.Collections.Generic.IDictionary<_,_> //|> System.Collections.Generic.Dictionary
    //    {|
    //        data = mapToMutDict this.data
    //        binaryData = mapToMutDict this.binaryData
    //        immutable = this.immutable
    //    |}
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            data = this.data |> Helpers.emptyAsNone |> Option.map Helpers.mapToIDictionary
            binaryData = this.binaryData |> Map.map (fun _ v -> v |> String.toBase64) |> Helpers.emptyAsNone |> Option.map Helpers.mapToIDictionary 
            immutable = this.immutable
        |}
        

type ConfigMapBuilder() =
    
    member _.Yield (_) = ConfigMap.empty
    
    member __.Zero () = ConfigMap.empty
    
    member __.Combine (currentValueFromYield: ConfigMap, accumulatorFromDelay: ConfigMap) = 
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            data = Helpers.mergeMap currentValueFromYield.data accumulatorFromDelay.data
            binaryData = Helpers.mergeMap currentValueFromYield.binaryData accumulatorFromDelay.binaryData
            immutable = Helpers.mergeBool (currentValueFromYield.immutable) (accumulatorFromDelay.immutable)
        }
    
    member __.Delay f = f()
    
    member this.For(state: ConfigMap , f: unit -> ConfigMap) =
        let delayed = f()
        this.Combine(state, delayed)

    // Metadata
    member this.Yield(name: string) = this.Name(ConfigMap.empty, name)
    
    /// Name of the ConfigMap. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: ConfigMap, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the ConfigMap.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: ConfigMap, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the ConfigMap
    [<CustomOperation "_labels">]
    member _.Labels(state: ConfigMap, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the ConfigMap
    [<CustomOperation "_annotations">]
    member _.Annotations(state: ConfigMap, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(ConfigMap.empty, metadata)
    /// Sets the ConfigMap metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: ConfigMap, metadata: Metadata) =
        { state with metadata = metadata }

    [<CustomOperation "add_data">]
    member _.AddData(state: ConfigMap, (key, value)) = { state with data = state.data |> Map.add key value }

    /// Sets ConfigMap data
    [<CustomOperation "data">]
    member _.Data(state: ConfigMap, data: (string*string) list) = 
        { state with data = data |> Map.ofList }

    /// Adds an item to the ConfigMap binary data
    [<CustomOperation "add_binaryData">]
    member _.AddBinaryData(state: ConfigMap, (key, value)) = { state with binaryData = state.binaryData |> Map.add key value }

    /// Sets ConfigMap binaryData
    [<CustomOperation "binaryData">]
    member _.BinaryData(state: ConfigMap, binaryData: (string*(byte array)) list) = 
        { state with binaryData = binaryData |> Map.ofList }
        
    /// Adds a file to the ConfigMap binary data
    [<CustomOperation "add_file">]
    member _.AddFileToBinaryData(state: ConfigMap, (key, filePath)) = 
        let bytes = System.IO.File.ReadAllBytes(filePath)
        { state with binaryData = state.binaryData |> Map.add key bytes }

//====================================
// CONFIGMAP LIST
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/config-map-v1/#ConfigMapList
//====================================

type ConfigMapList = ResourceList<ConfigMap>

//====================================
// Builder init
//====================================

[<AutoOpen>]
module ConfigMapBuilders =
    let configMap = new ConfigMapBuilder()
