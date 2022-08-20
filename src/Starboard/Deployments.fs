namespace Starboard.Resources.Deployments

open Starboard.Resources
open Starboard.Resources.Pods

/// Represents state that is later converted to a k8s Deployment Resource
/// https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
type Deployment = 
    { 
        pod: Pod option
        replicas: int
        ns: string 
        name: string option 
        labels: (string*string) list
        annotations: (string*string) list 
        selectors: LabelSelector }

type Deployment with
    static member Empty =
        { 
            pod = None
            replicas = 1
            name = None
            ns = "default"
            labels = List.empty
            annotations = List.empty
            selectors = LabelSelector.empty
        }
    member this.ToResource() =
        
        let mapValues f = function
                    | [] -> None
                    | xs -> Some (f xs)

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
                    matchLabels = mapValues mapToMatchLabels lbls
                    matchExpressions = mapValues mapToMatchExpressions exprs
                |} |> Some

        let toK8sMap lst =
            match lst with
            | [] -> None
            | lbls -> lbls |> dict |> Some
                    
        // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
        {|
            apiVersion = "apps/v1"
            kind = "Deployment"
            // https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
            metadata = {|
                name = this.name
                ``namespace`` = this.ns
                labels = toK8sMap this.labels
                annotations = toK8sMap this.annotations
            |}
            spec = {|
                replicas = this.replicas
                minReadySeconds = 0
                revisionHistoryLimit = 10
                progressDeadlineSeconds = 600
                selector = toK8sSelector()
                // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/
                template = {|
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
    member _.Name(state: Deployment, name: string) = { state with name = Some name}

    /// Namespace of the Deployment.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: Deployment, ns: string) = { state with ns = ns }
        
    /// Labels for the Deployment
    [<CustomOperation "labels">]
    member _.Labels(state: Deployment, labels: (string*string) list) = { state with labels = labels }

    /// Annotations for the Deployment
    [<CustomOperation "annotations">]
    member _.Annotations(state: Deployment, annotations: (string*string) list) = { state with annotations = annotations }
        
    /// Selector for the Deployment. Used for complex selections. Use `matchLabel(s)` for simple label matching.
    [<CustomOperation "selector">]
    member _.Selector(state: Deployment, selectors: LabelSelector) = { state with selectors = selectors }

    /// Selector for the Deployment. Used for complex selections. Use `matchLabel(s)` for simple label matching.
    [<CustomOperation "matchLabel">]
    member _.MatchLabel(state: Deployment, (key,value)) =
        { state with selectors = { state.selectors with matchLabels = List.append state.selectors.matchLabels [(key,value)] } }