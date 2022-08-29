namespace Starboard.Resources

open Starboard.Resources

/// Represents state that is later converted to a k8s Deployment Resource
/// https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
type Deployment = 
    { 
        pod: Pod option
        replicas: int
        metadata: Metadata
        selector: LabelSelector }

type Deployment with
    static member Empty =
        { 
            pod = None
            replicas = 1
            metadata = Metadata.empty
            selector = LabelSelector.empty
        }
    member this.K8sVersion() = "apps/v1"
    member this.K8sKind() = "Deployment"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
    member this.Spec() = 
        {|
            replicas = this.replicas
            minReadySeconds = 0
            revisionHistoryLimit = 10
            progressDeadlineSeconds = 600
            selector = LabelSelector.ToK8sModel this.selector
            // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/
            template = {|
                metadata = this.pod |> Option.bind (fun p -> p.K8sMetadata())
                // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/#PodTemplateSpec
                spec = {|
                    // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#Container
                    containers = this.pod |>  Option.map ( fun pod -> pod.containers |> List.map (fun c -> c.Spec()))
                |}
            |}
            strategy = None
        |}
    member this.ToResource() =
    
        // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
        {|
            apiVersion = this.K8sVersion()
            kind = "Deployment"
            // https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
            metadata = this.K8sMetadata()
            spec = this.Spec()
        |}


type DeploymentBuilder() =
    member _.Yield _ = Deployment.Empty

    [<CustomOperation "pod">]
    member _.Pods(state: Deployment, pod: Pod) = { state with pod = Some pod }
        
    [<CustomOperation "replicas">]
    member _.Replicas(state: Deployment, replicaCount: int) = { state with replicas = replicaCount }
        
    /// Name of the Deployment. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: Deployment, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}

    /// Namespace of the Deployment.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: Deployment, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
        
    /// Labels for the Deployment
    [<CustomOperation "labels">]
    member _.Labels(state: Deployment, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }

    /// Annotations for the Deployment
    [<CustomOperation "annotations">]
    member _.Annotations(state: Deployment, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
        
    /// Selector for the Deployment. Used for complex selections. Use `matchLabel(s)` for simple label matching.
    [<CustomOperation "selector">]
    member _.Selector(state: Deployment, selectors: LabelSelector) = { state with selector = selectors }

    /// Add a single label selector to the Deployment.
    [<CustomOperation "matchLabel">]
    member _.MatchLabel(state: Deployment, (key,value)) =
        { state with selector = { state.selector with matchLabels = List.append state.selector.matchLabels [(key,value)] } }

    /// Add multiple label selectors to the Deployment.
    [<CustomOperation "matchLabels">]
    member _.MatchLabels(state: Deployment, labels) =
        { state with selector = { state.selector with matchLabels = List.append state.selector.matchLabels labels } }
