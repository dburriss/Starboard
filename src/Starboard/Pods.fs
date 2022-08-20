namespace Starboard.Resources.Pods

open Starboard.Resources

type Pod = { 
    containers: Container list
    ns: string 
    name: string option 
    labels: (string*string) list
    annotations: (string*string) list 
}

type Pod with
    static member empty =
        { 
            containers = List.empty 
            ns = "default"
            name = None
            labels = List.empty
            annotations = List.empty
        }

// resource: version, kind, metadata, spec
// template: metadata, spec
    member this.K8sVersion() = "v1"
    member this.K8sKind() = "Pod"
    member this.K8sMetadata() = "Pod"
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
    member _.Name(state: Pod, name: string) = { state with name = Some name}

    /// Namespace of the Pod.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: Pod, ns: string) = { state with ns = ns }
        
    /// Labels for the Pod
    [<CustomOperation "labels">]
    member _.Labels(state: Pod, labels: (string*string) list) = { state with labels = labels }

    /// Annotations for the Pod
    [<CustomOperation "annotations">]
    member _.Annotations(state: Pod, annotations: (string*string) list) = { state with annotations = annotations }