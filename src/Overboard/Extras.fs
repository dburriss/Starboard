namespace Overboard.Extras

open System
open Overboard.Common
open Overboard.Workload
open Overboard.Storage
open Overboard

type FsharpJob = {
    metadata: Metadata
    files: string list
    entryPoint: string option
    image: string
    restartPolicy: RestartPolicy
    schedule: string option
}
type FsharpJob with
    static member empty = {
        metadata = Metadata.empty
        files = List.empty
        entryPoint = None
        image = "mcr.microsoft.com/dotnet/sdk:7.0-alpine"
        restartPolicy = Never
        schedule = None
    }
    static member finalFiles (job: FsharpJob) =
        match (job.entryPoint, job.files) with
        | Some entry, xs when xs |> List.contains entry |> not -> List.append [entry] xs
        | _, xs -> xs

    static member finalEntrypoint (job: FsharpJob) =
        match job.entryPoint with
        | Some entry -> entry
        | None -> job.files |> List.tryFind Files.isFsxFile |> Option.defaultValue ""

type FsharpJobBuilder() =

    member _.Yield _ = FsharpJob.empty

    // Metadata
    member this.Yield(name: string) = this.Name(FsharpJob.empty, name)
    
    /// Name of the FsharpJob. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: FsharpJob, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the FsharpJob.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: FsharpJob, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the FsharpJob
    [<CustomOperation "_labels">]
    member _.Labels(state: FsharpJob, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the FsharpJob
    [<CustomOperation "_annotations">]
    member _.Annotations(state: FsharpJob, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(FsharpJob.empty, metadata)
    /// Sets the FsharpJob metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: FsharpJob, metadata: Metadata) =
        { state with metadata = metadata }

    [<CustomOperation "files">]
    member _.Files(state: FsharpJob, files: string list) = 
        { state with files = files }
    
    [<CustomOperation "entryPoint">]
    member _.EntryPoint(state: FsharpJob, entryPoint: string) = 
        { state with entryPoint = Some entryPoint }
    
    [<CustomOperation "image">]
    member _.Image(state: FsharpJob, image: string) = 
        { state with image = image }
    
    [<CustomOperation "restartPolicy">]
    member _.RestartPolicy(state: FsharpJob, restartPolicy: RestartPolicy) = 
        { state with restartPolicy = restartPolicy }

    [<CustomOperation "schedule">]
    member _.Schedule(state: FsharpJob, schedule: string) = { state with schedule = Some schedule }
    
    member this.Run(state: FsharpJob) =
        let normalize (s: string) =
            s.ToCharArray()
            |> Array.filter (fun c -> Char.IsLetterOrDigit(c) || c = '-')
            |> String
            |> String.lower

        let entryPoint = state |> FsharpJob.finalEntrypoint
        
        let name =
            state.metadata.name
            |> Option.orElseWith( fun () -> entryPoint |> Files.fileNameWithoutExt |> String.lower |> Some )
            |> Option.map normalize
            |> Option.defaultValue "fsx"

        let configMapName = $"script-{name}-configmap"
        let jobName = $"script-{name}-job"
        let podName = $"script-{name}-pod"
        let containerName = $"script-{name}-container"
        let imageName = state.image
        let labels = state.metadata.labels
        let includeFiles = state |> FsharpJob.finalFiles |> List.map (fun path -> (Files.fileName path, path))
        
        let isCron job = job.schedule.IsSome
        
        let fsConfigMap = configMap {
            _name configMapName
            files includeFiles
        }
        let fsJob = job {
            _name jobName
            ttlSecondsAfterFinished 300
            pod {
                _name podName
                _labels labels
                restartPolicy state.restartPolicy
                container {
                    name containerName
                    image imageName
                    command ["dotnet"]
                    args ["fsi"; entryPoint]
                    workingDir "/scripts"
                    volumeMount {
                        name "script-volume"
                        mountPath "/scripts"
                    }
                }
                configMapVolume {
                    name "script-volume"
                    configName configMapName
                }
            }
        }
        if isCron state then
            let cronSchedule = state.schedule |> Option.defaultValue "0 * * * *"
            let fsCron = cronJob {
                _name jobName
                jobTemplate fsJob
                schedule cronSchedule
            }
            k8s {
                fsConfigMap
                fsCron
            }
        else 
            k8s { 
                fsConfigMap
                fsJob            
            }
     
[<AutoOpen>]
module ExtrasBuilders =
    let fsJob = new FsharpJobBuilder()