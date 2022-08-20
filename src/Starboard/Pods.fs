namespace Starboard.Resources.Pods

open Starboard.Resources

type Pod = { 
    containers: Container list
    metadata: Metadata
}

type Pod with
    static member empty =
        { 
            containers = List.empty 
            metadata = Metadata.empty
        }

// resource: version, kind, metadata, spec
// template: metadata, spec
    member this.K8sVersion() = "v1"
    member this.K8sKind() = "Pod"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
        

type PodBuilder() =
        
    member _.Yield (_) = Pod.empty

    [<CustomOperation "container">]
    member _.Container(state: Pod, container: Container) = { state with containers = List.append state.containers [container] }
        
    [<CustomOperation "containers">]
    member _.Containers(state: Pod, containers: Container list) = { state with containers = containers }

    /// Name of the Pod. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: Pod, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata }

    /// Namespace of the Pod.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: Pod, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
        
    /// Labels for the Pod
    [<CustomOperation "labels">]
    member _.Labels(state: Pod, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }

    /// Annotations for the Pod
    [<CustomOperation "annotations">]
    member _.Annotations(state: Pod, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }