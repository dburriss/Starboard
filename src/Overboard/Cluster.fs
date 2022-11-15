namespace Overboard.Cluster

open Overboard.Common

type Namespace = {
    metadata: Metadata
}
type Namespace with
    static member empty = {
        metadata = Metadata.empty
    }

type Namespace with
    member _.K8sVersion() = "v1"
    member _.K8sKind() = "Namespace"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
        |}

type NamespaceBuilder() =
    member _.Yield _ = Namespace.empty

    // Metadata
    member this.Yield(name: string) = this.Name(Namespace.empty, name)
    
    /// Name of the Namespace. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: Namespace, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the Namespace.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: Namespace, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the Namespace
    [<CustomOperation "_labels">]
    member _.Labels(state: Namespace, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the Namespace
    [<CustomOperation "_annotations">]
    member _.Annotations(state: Namespace, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(Namespace.empty, metadata)
    /// Sets the Namespace metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: Namespace, metadata: Metadata) =
        { state with metadata = metadata }

[<AutoOpen>]
module NamespaceBuilders =    
    let ns = new NamespaceBuilder()