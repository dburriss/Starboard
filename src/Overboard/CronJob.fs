namespace Overboard.Workload

open Overboard
open Overboard.Common

type ConcurrencyPolicy = | Allow | Forbid | Replace

type CronJob = {
    metadata: Metadata
    jobTemplate: Job option
    schedule: string option
    timeZone: string
    concurrencyPolicy: ConcurrencyPolicy
    startingDeadlineSeconds: int64 option
    suspend: bool
    successfulJobsHistoryLimit: int
    failedJobsHistoryLimit: int
}

type CronJob with
    static member empty = {
        metadata = Metadata.empty
        jobTemplate = None
        schedule = None
        timeZone = "Etc/UTC"
        concurrencyPolicy = Allow
        startingDeadlineSeconds = None
        suspend = false
        successfulJobsHistoryLimit = 3
        failedJobsHistoryLimit = 1
    }
    member this.K8sVersion() = "batch/v1"
    member this.K8sKind() = "CronJob"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.Spec() = 
        {|
            // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/
            jobTemplate = {|
                metadata = this.jobTemplate |> Option.map (fun p -> p.K8sMetadata())
                // https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-template-v1/#PodTemplateSpec
                spec = this.jobTemplate |> Option.map (fun p -> p.Spec())
            |}
            schedule = this.schedule
            timeZone = this.timeZone
            concurrencyPolicy = this.concurrencyPolicy.ToString()
            startingDeadlineSeconds = this.startingDeadlineSeconds
            suspend = this.suspend
            successfulJobsHistoryLimit = this.successfulJobsHistoryLimit
            failedJobsHistoryLimit = this.failedJobsHistoryLimit
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
        @ (Validation.required (fun x -> x.jobTemplate) $"{kind} `jobTemplate` is required." this)
        @ (Validation.required (fun x -> x.schedule) $"{kind} `schedule` is required." this)

type CronJobBuilder() =
    member _.Yield _ = CronJob.empty

    member __.Zero () = CronJob.empty
    
    member __.Combine (currentValueFromYield: CronJob, accumulatorFromDelay: CronJob) = 
        let mergeConcurrencyPolicy v1 v2 =
            match (v1,v2) with
            | Allow, v2' -> v2'
            | v1', Allow -> v1'
            | _ -> v1

        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            jobTemplate = Helpers.mergeOption (currentValueFromYield.jobTemplate) (accumulatorFromDelay.jobTemplate)
            schedule = Helpers.mergeOption (currentValueFromYield.schedule) (accumulatorFromDelay.schedule)
            timeZone = Helpers.mergeString (currentValueFromYield.timeZone) (accumulatorFromDelay.timeZone)
            concurrencyPolicy = mergeConcurrencyPolicy (currentValueFromYield.concurrencyPolicy) (accumulatorFromDelay.concurrencyPolicy)
            startingDeadlineSeconds = Helpers.mergeOption (currentValueFromYield.startingDeadlineSeconds) (accumulatorFromDelay.startingDeadlineSeconds)
            suspend = Helpers.mergeBool (currentValueFromYield.suspend) (accumulatorFromDelay.suspend)
            successfulJobsHistoryLimit = Helpers.mergeInt32 (currentValueFromYield.successfulJobsHistoryLimit) (accumulatorFromDelay.successfulJobsHistoryLimit)
            failedJobsHistoryLimit = Helpers.mergeInt32 (currentValueFromYield.failedJobsHistoryLimit) (accumulatorFromDelay.failedJobsHistoryLimit)
            
        }
    
    member __.Delay f = f()
    
    member this.For(state: CronJob , f: unit -> CronJob) =
        let delayed = f()
        this.Combine(state, delayed)
    
    // Metadata
    member this.Yield(name: string) = this.Name(CronJob.empty, name)
    
    /// Name of the CronJob. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: CronJob, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the CronJob.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: CronJob, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the CronJob
    [<CustomOperation "_labels">]
    member _.Labels(state: CronJob, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the CronJob
    [<CustomOperation "_annotations">]
    member _.Annotations(state: CronJob, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(CronJob.empty, metadata)
    /// Sets the CronJob metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: CronJob, metadata: Metadata) =
        { state with metadata = metadata }
    
    member this.Yield(job: Job) = this.JobTemplate(CronJob.empty, job)
    [<CustomOperation "jobTemplate">]
    member _.JobTemplate(state: CronJob, job: Job) = { state with jobTemplate = Some job }
    
    [<CustomOperation "schedule">]
    member _.Schedule(state: CronJob, schedule: string) = { state with schedule = Some schedule }
    
    [<CustomOperation "timeZone">]
    member _.TimeZone(state: CronJob, timeZone: string) = { state with timeZone = timeZone }
    
    [<CustomOperation "concurrencyPolicy">]
    member _.ConcurrencyPolicy(state: CronJob, concurrencyPolicy: ConcurrencyPolicy) = { state with concurrencyPolicy = concurrencyPolicy }
    
    [<CustomOperation "startingDeadlineSeconds">]
    member _.StartingDeadlineSeconds(state: CronJob, startingDeadlineSeconds: int64) = { state with startingDeadlineSeconds = Some startingDeadlineSeconds }
    
    [<CustomOperation "suspend">]
    member _.Suspend(state: CronJob) = { state with suspend = true }

    [<CustomOperation "successfulJobsHistoryLimit">]
    member _.SuccessfulJobsHistoryLimit(state: CronJob, successfulJobsHistoryLimit: int) = { state with successfulJobsHistoryLimit = successfulJobsHistoryLimit }
    
    [<CustomOperation "failedJobsHistoryLimit">]
    member _.FailedJobsHistoryLimit(state: CronJob, failedJobsHistoryLimit: int) = { state with failedJobsHistoryLimit = failedJobsHistoryLimit }
    
[<AutoOpen>]
module CronJobBuilders =
    let cronJob = new CronJobBuilder()