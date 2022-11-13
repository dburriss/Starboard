namespace Overboard.Storage

open Overboard
open Overboard.Common

//====================================
// Secrets
// Ref: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/secret-v1/
// API: https://v1-24.docs.kubernetes.io/docs/reference/generated/kubernetes-api/v1.24/#secret-v1-core
//====================================

type Secret = { 
    metadata: Metadata
    stringData: Map<string,string>
    data: Map<string,byte array>
    immutable: bool
    secretType: string
}

type Secret with
    static member empty =
        { 
            metadata = Metadata.empty
            stringData = Map.empty
            data = Map.empty
            immutable = false
            secretType = "Opaque"
        }

// resource: version, kind, metadata, spec
// template: metadata, spec
    member this.K8sVersion() = "v1"
    member this.K8sKind() = "Secret"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.ToResource() =

        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            stringData = this.stringData |> Helpers.emptyAsNone |> Option.map Helpers.mapToIDictionary
            data = this.data |> Map.map (fun _ v -> v |> String.toBase64) |> Helpers.emptyAsNone |> Option.map Helpers.mapToIDictionary 
            immutable = this.immutable
            ``type`` = this.secretType
        |}
        

type SecretBuilder() =
    
    member _.Yield (_) = Secret.empty
    
    member __.Zero () = Secret.empty
    
    member __.Combine (currentValueFromYield: Secret, accumulatorFromDelay: Secret) = 
       
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            stringData = Helpers.mergeMap (currentValueFromYield.stringData) (accumulatorFromDelay.stringData)
            data =  Helpers.mergeMap (currentValueFromYield.data) (accumulatorFromDelay.data)
            immutable = Helpers.mergeBool (currentValueFromYield.immutable) (accumulatorFromDelay.immutable)
            secretType = Helpers.mergeString (currentValueFromYield.secretType) (accumulatorFromDelay.secretType)
        }
    
    member __.Delay f = f()
    
    member this.For(state: Secret , f: unit -> Secret) =
        let delayed = f()
        this.Combine(state, delayed)
    

    // Metadata
    member this.Yield(name: string) = this.Name(Secret.empty, name)
    
    /// Name of the Secret. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: Secret, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the Secret.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: Secret, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the Secret
    [<CustomOperation "_labels">]
    member _.Labels(state: Secret, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the Secret
    [<CustomOperation "_annotations">]
    member _.Annotations(state: Secret, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(Secret.empty, metadata)
    /// Sets the Secret metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: Secret, metadata: Metadata) =
        { state with metadata = metadata }

    /// Sets Secret to immutable
    [<CustomOperation "immutable">]
    member _.Immutable(state: Secret) = { state with immutable = true }
    
    /// Adds an item to the Secret stringdata
    [<CustomOperation "add_stringData">]
    member _.AddStringData(state: Secret, (key, value)) = { state with stringData = state.stringData |> Map.add key value }

    /// Sets Secret string data
    [<CustomOperation "stringData">]
    member _.StringData(state: Secret, data: (string*string) list) = 
        { state with stringData = data |> Map.ofList }
        
    /// Adds an item to the Secret data
    [<CustomOperation "add_data">]
    member _.AddData(state: Secret, (key, value)) = { state with data = state.data |> Map.add key value }

    /// Sets Secret binary data
    [<CustomOperation "data">]
    member _.Data(state: Secret, binaryData: (string*(byte array)) list) = 
        { state with data = binaryData |> Map.ofList }

    /// Adds a file to the ConfigMap binary data
    [<CustomOperation "add_file">]
    member _.AddFileToBinaryData(state: Secret, (key, filePath)) = 
        let bytes = System.IO.File.ReadAllBytes(filePath)
        { state with data = state.data |> Map.add key bytes }


//====================================
// CONFIGMAP LIST
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/config-map-v1/#ConfigMapList
//====================================

type SecretList = ResourceList<Secret>

//====================================
// Builder init
//====================================

[<AutoOpen>]
module SecretBuilders =
    let secret = new SecretBuilder()