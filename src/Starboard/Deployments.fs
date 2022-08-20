namespace Starboard.Resources.Deployments

open Starboard.Resources
open Starboard.Resources.Pods

/// Represents state that is later converted to a k8s Deployment Resource
/// https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
type Deployment = 
    { 
        pod: Pod option
        replicas: int
        metadata: Metadata
        selectors: LabelSelector }

type Deployment with
    static member Empty =
        { 
            pod = None
            replicas = 1
            metadata = Metadata.empty
            selectors = LabelSelector.empty
        }
    member this.ToResource() =

        let toK8sSelector() =
            let matchLabels = this.selectors.matchLabels
            let matchExpressions = this.selectors.matchExpressions

            let mapToMatchLabels lst = dict lst
            let mapToMatchExpressions labelSelectors =
                labelSelectors
                |> List.map (fun e ->  {|
                                            key = e.key
                                            operator = e.operator.ToString()
                                            values = e.values
                                        |})

            match (matchLabels, matchExpressions) with
            | [], [] -> None
            | lbls, exprs -> 
                {|
                    matchLabels = Helpers.mapValues mapToMatchLabels lbls
                    matchExpressions = Helpers.mapValues mapToMatchExpressions exprs
                |} |> Some

        
            //match lst with
            //| [] -> None
            //| lbls -> lbls |> dict |> Some
                    
        // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
        {|
            apiVersion = "apps/v1"
            kind = "Deployment"
            // https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
            metadata = Metadata.ToK8sModel this.metadata
            spec = {|
                replicas = this.replicas
                minReadySeconds = 0
                revisionHistoryLimit = 10
                progressDeadlineSeconds = 600
                selector = toK8sSelector()
                // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/
                template = {|
                    metadata = this.pod |> Option.bind (fun p -> p.K8sMetadata())
                    // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/#PodTemplateSpec
                    spec = {|
                        // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#Container
                        containers = this.pod |>  Option.map ( fun pod -> [
                            for c in pod.containers do
                                {|
                                    name = c.name
                                    image = c.image
                                    command = c.command
                                    args = c.args
                                |}
                        ])
                    |}
                |}
                strategy = None
            |}
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
    member _.Selector(state: Deployment, selectors: LabelSelector) = { state with selectors = selectors }

    /// Add a single label selector to the Deployment.
    [<CustomOperation "matchLabel">]
    member _.MatchLabel(state: Deployment, (key,value)) =
        { state with selectors = { state.selectors with matchLabels = List.append state.selectors.matchLabels [(key,value)] } }

    /// Add multiple label selectors to the Deployment.
    [<CustomOperation "matchLabels">]
    member _.MatchLabels(state: Deployment, labels) =
        { state with selectors = { state.selectors with matchLabels = List.append state.selectors.matchLabels labels } }
