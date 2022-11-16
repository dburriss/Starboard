namespace Overboard.Workload

open Overboard
open Overboard.Common

type CompletionMode = | NonIndexed | Indexed

type Job = {
    metadata: Metadata
    template: Pod option
    parallelism: int
    completions: int
    completionMode: CompletionMode
    backoffLimit: int
    activeDeadlineSeconds: int64 option
    ttlSecondsAfterFinished: int
    suspend: bool
    selector: LabelSelector option
    manualSelector: bool
}

type Job with
    static member empty = {
        metadata = Metadata.empty
        template = None
        parallelism = 1
        completions = 1
        completionMode = NonIndexed
        backoffLimit = 6
        activeDeadlineSeconds = None
        ttlSecondsAfterFinished = 30
        suspend = false
        selector = None
        manualSelector = false
    }
    member this.K8sVersion() = "batch/v1"
    member this.K8sKind() = "Job"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.Spec() = 
        {|
            parallelism = this.parallelism
            completions = this.completions
            completionMode = this.completionMode.ToString()
            backoffLimit = this.backoffLimit
            activeDeadlineSeconds = this.activeDeadlineSeconds
            ttlSecondsAfterFinished = this.ttlSecondsAfterFinished
            suspend = this.suspend
            selector = this.selector |> Option.bind LabelSelector.ToK8sModel 
            manualSelector = this.manualSelector
            // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/
            template = {|
                metadata = this.template |> Option.map (fun p -> p.K8sMetadata())
                // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/#PodTemplateSpec
                spec = this.template |> Option.map (fun p -> p.Spec())
            |}
            strategy = None
        |}
    member this.ToResource() =
    
        // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/deployment-v1/
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            // https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
            metadata = this.K8sMetadata()
            spec = this.Spec()
        |}
    member this.Validate() =
        let kind = this.K8sKind()
        (this.metadata.Validate(kind))
        @ (Validation.required (fun x -> x.template) $"{kind} `template` is required." this)

type JobBuilder() =
    member _.Yield _ = Job.empty

    member __.Zero () = Job.empty
    
    member __.Combine (currentValueFromYield: Job, accumulatorFromDelay: Job) = 
        let mergeCompletionMode v1 v2 =
            match (v1,v2) with
            | NonIndexed, v2' -> v2'
            | v1', NonIndexed -> v1'
            | _ -> v1

        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            template = Helpers.mergeOption (currentValueFromYield.template) (accumulatorFromDelay.template)
            parallelism = Helpers.mergeInt32 (currentValueFromYield.parallelism) (accumulatorFromDelay.parallelism)
            completions = Helpers.mergeInt32 (currentValueFromYield.completions) (accumulatorFromDelay.completions)
            completionMode = mergeCompletionMode currentValueFromYield.completionMode accumulatorFromDelay.completionMode
            backoffLimit = Helpers.mergeInt32 (currentValueFromYield.backoffLimit) (accumulatorFromDelay.backoffLimit)
            activeDeadlineSeconds = Helpers.mergeOption (currentValueFromYield.activeDeadlineSeconds) (accumulatorFromDelay.activeDeadlineSeconds)
            suspend = Helpers.mergeBool (currentValueFromYield.suspend) (accumulatorFromDelay.suspend)
            selector = Helpers.mergeOption currentValueFromYield.selector accumulatorFromDelay.selector
            manualSelector = Helpers.mergeBool (currentValueFromYield.manualSelector) (accumulatorFromDelay.manualSelector)
        }
    
    member __.Delay f = f()
    
    member this.For(state: Job , f: unit -> Job) =
        let delayed = f()
        this.Combine(state, delayed)
    
    // Metadata
    member this.Yield(name: string) = this.Name(Job.empty, name)
    
    /// Name of the Job. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: Job, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the Job.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: Job, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the Job
    [<CustomOperation "_labels">]
    member _.Labels(state: Job, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the Job
    [<CustomOperation "_annotations">]
    member _.Annotations(state: Job, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(Job.empty, metadata)
    /// Sets the Job metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: Job, metadata: Metadata) =
        { state with metadata = metadata }
    
    member this.Yield(pod: Pod) = this.Pod(Job.empty, pod)
    [<CustomOperation "template">]
    member _.Pod(state: Job, pod: Pod) = { state with template = Some pod }
        
    [<CustomOperation "parallelism">]
    member _.Parallelism(state: Job, parallelism: int) = { state with parallelism = parallelism }
    
    [<CustomOperation "completions">]
    member _.Completions(state: Job, completions: int) = { state with completions = completions }
    
    [<CustomOperation "completionMode">]
    member _.CompletionMode(state: Job, completionMode: CompletionMode) = { state with completionMode = completionMode }
    
    [<CustomOperation "backoffLimit">]
    member _.BackoffLimit(state: Job, backoffLimit: int) = { state with backoffLimit = backoffLimit }
    [<CustomOperation "activeDeadlineSeconds">]
    member _.ActiveDeadlineSeconds(state: Job, activeDeadlineSeconds: int64) = { state with activeDeadlineSeconds = Some activeDeadlineSeconds  }
    
    [<CustomOperation "ttlSecondsAfterFinished">]
    member _.TtlSecondsAfterFinished(state: Job, ttlSecondsAfterFinished: int) = { state with ttlSecondsAfterFinished = ttlSecondsAfterFinished }
    
    [<CustomOperation "suspend">]
    member _.Suspend(state: Job) = { state with suspend = true }

    // Selector
    member this.Yield(selector: LabelSelector) = this.Selector(Job.empty, selector)
    [<CustomOperation "selector">]
    member _.Selector(state: Job, selector: LabelSelector) = { state with selector = Some selector }
    
    [<CustomOperation "manualSelector">]
    member _.ManualSelector(state: Job) = { state with manualSelector = true }
    
[<AutoOpen>]
module JobBuilders =
    let job = new JobBuilder()