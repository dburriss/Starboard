namespace Overboard.Storage

open Overboard
open Overboard.Common

// Volumes
// TODO: CSI

// VOLUMES
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/volume/
type KeyToPath = {
    key: string
    path: string
    mode: int option
}
type KeyToPath with
    static member fromKeyValue(key, path) =
        { 
            key = key
            path = path
            mode = None
        }

type PersistentVolumeClaimVolumeSource = {
    claimName: string
    readOnly: bool
}

type PersistentVolumeClaimVolumeSource with
    static member empty =
        {
            claimName = ""
            readOnly = false
        }
    member this.Spec() = this
    
type ConfigMapVolumeSource = {
    name: string option
    optional: bool
    defaultMode: int option
    items: KeyToPath list
}
type ConfigMapVolumeSource with
    static member empty =
        { 
            name = None
            optional = false
            defaultMode = None
            items = List.empty
        }
    member this.Spec() =
        {|
            name = this.name
            optional = this.optional
            defaultMode = this.defaultMode
            items = this.items
        |}

type SecretVolumeSource = {
    secretName: string option
    optional: bool
    defaultMode: int option
    items: KeyToPath list
}
type SecretVolumeSource with
    static member empty =
        { 
            secretName = None
            optional = false
            defaultMode = None
            items = List.empty
        }
    member this.Spec() =
        {|
            secretName = this.secretName
            optional = this.optional
            defaultMode = this.defaultMode
            items = this.items
        |}
    
type EmptyDirVolumeSource = {
    medium: string
    sizeLimit: int<Mi>
}
type EmptyDirVolumeSource with
    static member empty =
        { 
            medium = ""
            sizeLimit = 0<Mi> 
        }
    member this.Spec() =
        {|
            medium = this.medium
            sizeLimit = sprintf "%iMi" this.sizeLimit
        |}
    
/// hostPath represents a directory on the host
/// See: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-v1/#local
type HostPathType = 
    | Default | DirectoryOrCreate | Directory | FileOrCreate | File | Socket | CharDevice | BlockDevice
    with override this.ToString() =
            match this with
            | Default -> ""
            | DirectoryOrCreate -> "DirectoryOrCreate"
            | Directory -> "Directory"
            | FileOrCreate -> "FileOrCreate"
            | File -> "File"
            | Socket -> "Socket"
            | CharDevice -> "CharDevice"
            | BlockDevice -> "BlockDevice"
        
type HostPathVolumeSource = {
    hostPathType: HostPathType
    path: string
}
type HostPathVolumeSource with
    static member empty =
        { 
            hostPathType = Default
            path = "" 
        }
    member this.Spec() =
        {|
            path = this.path
            ``type`` = this.hostPathType.ToString()
        |}

type CsiVolumeSource = {
    driver: string
    fsType: string
    nodePublishSecretRef: LocalObjectReference option
    readOnly: bool
    volumeAttributes: (string*string) list
}
type CsiVolumeSource with
    static member empty =
        { 
            driver = ""
            fsType = ""
            nodePublishSecretRef = None
            readOnly = false
            volumeAttributes = List.empty
        }
    member this.Spec() =
        {|
            driver = this.driver
            fsType = this.fsType
            nodePublishSecretRef = this.nodePublishSecretRef
            readOnly = this.readOnly
            volumeAttributes = 
                this.volumeAttributes
                |> Helpers.emptyAsNone
                |> Option.map Map.ofList
                |> Option.map Helpers.mapToIDictionary
        |}

type VolumeType = 
    // Exposed Persistent volumes
    | PersistentVolumeClaimVolume of PersistentVolumeClaimVolumeSource
    // Projections
    | ConfigMapVolume of ConfigMapVolumeSource
    | SecretVolume of SecretVolumeSource
    // Local
    | EmptyDirVolume of EmptyDirVolumeSource
    | HostPathVolume of HostPathVolumeSource
    // Persistent
    | CsiVolume of CsiVolumeSource


type Volume = {
    name: string 
    volume: VolumeType
}

type Volume with
    static member empty = 
        {
            name = ""
            volume = EmptyDirVolume EmptyDirVolumeSource.empty
        }
    member this.Spec() =
        match this.volume with
        | PersistentVolumeClaimVolume v ->
            {|
                name = this.name
                persistentVolumeClaim = Some (v.Spec())
                configMap = None
                secret = None
                emptyDir = None
                hostPath = None
                csi = None
            |}
        | ConfigMapVolume v ->
            {|
                name = this.name
                persistentVolumeClaim = None
                configMap = Some (v.Spec())
                secret = None
                emptyDir = None
                hostPath = None
                csi = None
            |}        
        | SecretVolume v ->
            {|
                name = this.name
                persistentVolumeClaim = None
                configMap = None
                secret = Some (v.Spec())
                emptyDir = None
                hostPath = None
                csi = None
            |}
        | EmptyDirVolume v ->
            {|
                name = this.name
                persistentVolumeClaim = None
                configMap = None
                secret = None
                emptyDir = Some (v.Spec())
                hostPath = None
                csi = None
            |}
        | HostPathVolume v ->
            {|
                name = this.name
                persistentVolumeClaim = None
                configMap = None
                secret = None
                emptyDir = None
                hostPath = Some (v.Spec())
                csi = None
            |}
        | CsiVolume v ->
            {|
                name = this.name
                persistentVolumeClaim = None
                configMap = None
                secret = None
                emptyDir = None
                hostPath = None
                csi = Some (v.Spec())
            |}
    member this.Validate() =
        let kind = "Volume"
        let validatePersistentVolumeClaimVolume = function 
            | PersistentVolumeClaimVolume v -> Validation.notEmpty (fun x -> x.claimName) $"{kind} `persistentVolumeClaim.claimName` is required." v 
            | _ -> []

        let validateHostPathVolume = function 
            | HostPathVolume v -> Validation.notEmpty (fun x -> x.path) $"{kind} `hostPath.path` is required." v 
            | _ -> []

        let validateCsiVolume = function 
            | CsiVolume v -> Validation.notEmpty (fun x -> x.driver) $"{kind} `csi.driver` is required." v 
            | _ -> []

        Validation.notEmpty (fun x -> x.name) $"{kind} `name` is required." this
        @ validatePersistentVolumeClaimVolume this.volume
        @ validateHostPathVolume this.volume
        @ validateCsiVolume this.volume


// =============================
// PVC
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/volume/#exposed-persistent-volumes
// =============================
type PersistentVolumeClaimVolumeBuilder() =
    member _.Yield _ = { Volume.empty with volume = PersistentVolumeClaimVolume PersistentVolumeClaimVolumeSource.empty }
    
    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        { state with name = name }

    [<CustomOperation "claimName">]
    member _.SizeLimit(state: Volume, claimName: string) =   
        match state.volume with
        | PersistentVolumeClaimVolume v -> 
            let newVolume = PersistentVolumeClaimVolume { v with claimName = claimName }
            { state with volume = newVolume }
        | _ -> state
    
    [<CustomOperation "readOnly">]
    member _.ReadOnly(state: Volume) =   
        match state.volume with
        | PersistentVolumeClaimVolume v -> 
            let newVolume = PersistentVolumeClaimVolume { v with readOnly = true }
            { state with volume = newVolume }
        | _ -> state

// =============================
// Projections
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/volume/#projections
// =============================
type ConfigMapVolumeBuilder() =
    member _.Yield _ = { Volume.empty with volume = ConfigMapVolume ConfigMapVolumeSource.empty }

    member this.Yield(name: string) = 
        {
            name = name
            volume = ConfigMapVolume ConfigMapVolumeSource.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        { state with name = name }

    [<CustomOperation "configName">]
    member _.ConfigName(state: Volume, name: string) = 
        match state.volume with
        | ConfigMapVolume v -> 
            let newVolume = ConfigMapVolume { v with name = Some name }
            { state with volume = newVolume }
        | _ -> state
        
    [<CustomOperation "optional">]
    member _.Optional(state: Volume) =
        match state.volume with
        | ConfigMapVolume v -> 
            let newVolume = ConfigMapVolume { v with optional = true }
            { state with volume = newVolume }
        | _ -> state
   
    [<CustomOperation "defaultMode">]
    member _.DefaultMode(state: Volume, mode: int) =
        match state.volume with
        | ConfigMapVolume v -> 
            let newVolume = ConfigMapVolume { v with defaultMode = Some mode }
            { state with volume = newVolume }
        | _ -> state
    
    [<CustomOperation "item">]
    member _.Item(state: Volume, keyValue) =
        match state.volume with
        | ConfigMapVolume v -> 
            let newVolume = ConfigMapVolume { v with items = List.append v.items [KeyToPath.fromKeyValue(keyValue)] }
            { state with volume = newVolume }
        | _ -> state

type SecretVolumeBuilder() =
    member _.Yield _ = { Volume.empty with volume = SecretVolume SecretVolumeSource.empty }

    member this.Yield(name: string) = 
        {
            name = name
            volume = SecretVolume SecretVolumeSource.empty
        }

    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        { state with name = name }

    [<CustomOperation "secretName">]
    member _.SecretName(state: Volume, name: string) = 
        match state.volume with
        | SecretVolume v -> 
            let newVolume = SecretVolume { v with secretName = Some name }
            { state with volume = newVolume }
        | _ -> state
        
    [<CustomOperation "optional">]
    member _.Optional(state: Volume) =
        match state.volume with
        | SecretVolume v -> 
            let newVolume = SecretVolume { v with optional = true }
            { state with volume = newVolume }
        | _ -> state
   
    [<CustomOperation "defaultMode">]
    member _.DefaultMode(state: Volume, mode: int) =
        match state.volume with
        | SecretVolume v -> 
            let newVolume = SecretVolume { v with defaultMode = Some mode }
            { state with volume = newVolume }
        | _ -> state
    
    [<CustomOperation "item">]
    member _.Item(state: Volume, keyValue) =
        match state.volume with
        | SecretVolume v -> 
            let newVolume = SecretVolume { v with items = List.append v.items [KeyToPath.fromKeyValue(keyValue)] }
            { state with volume = newVolume }
        | _ -> state

// =============================
// Locals
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/volume/#local-temporary-directory
// =============================
type EmptyDirVolumeBuilder() =
    member _.Yield _ = { Volume.empty with volume = EmptyDirVolume EmptyDirVolumeSource.empty }
    
    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        { state with name = name }

    [<CustomOperation "useMemory">]
    member _.UseMemory(state: Volume) =   
        match state.volume with
        | EmptyDirVolume v -> 
            let newVolume = EmptyDirVolume { v with medium = "Memory" }
            { state with volume = newVolume }
        | _ -> state

    [<CustomOperation "sizeLimit">]
    member _.SizeLimit(state: Volume, size: int<Mi>) =   
        match state.volume with
        | EmptyDirVolume v -> 
            let newVolume = EmptyDirVolume { v with sizeLimit = size }
            { state with volume = newVolume }
        | _ -> state

type HostPathVolumeBuilder() =
    member _.Yield _ = { Volume.empty with volume = HostPathVolume HostPathVolumeSource.empty }
    
    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        { state with name = name }

    [<CustomOperation "path">]
    member _.Path(state: Volume, path: string) =   
        match state.volume with
        | HostPathVolume v -> 
            let newVolume = HostPathVolume { v with path = path }
            { state with volume = newVolume }
        | _ -> state

    [<CustomOperation "hostPathType">]
    member _.Type(state: Volume, hostPathType: HostPathType) =   
        match state.volume with
        | HostPathVolume v -> 
            let newVolume = HostPathVolume { v with hostPathType = hostPathType }
            { state with volume = newVolume }
        | _ -> state

// =============================
// Persistent volumes
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/volume/#persistent-volumes
// =============================
type CsiVolumeBuilder() =
    member _.Yield _ = { Volume.empty with volume = CsiVolume CsiVolumeSource.empty }
    
    member __.Zero () = { Volume.empty with volume = CsiVolume CsiVolumeSource.empty }
    
    member __.Combine (currentValueFromYield: Volume, accumulatorFromDelay: Volume) = 
        let combineVolume (v1: VolumeType) (v2: VolumeType) =
            match (v1,v2) with
            | CsiVolume vol1, CsiVolume vol2 ->
                { vol1 with 
                    driver = Helpers.mergeString (vol1.driver) (vol2.driver)
                    fsType = Helpers.mergeString (vol1.fsType) (vol2.fsType)
                    nodePublishSecretRef = Helpers.mergeOption (vol1.nodePublishSecretRef) (vol2.nodePublishSecretRef)
                    readOnly = Helpers.mergeBool (vol1.readOnly) (vol2.readOnly)
                    volumeAttributes = List.append vol1.volumeAttributes vol2.volumeAttributes
                } |> CsiVolume
            | _ -> failwithf "Unexpected volume types in builder %A" (v1, v2)
        { currentValueFromYield with
            name = Helpers.mergeString (currentValueFromYield.name) (accumulatorFromDelay.name)
            volume = combineVolume (currentValueFromYield.volume) (accumulatorFromDelay.volume)
        }
    
    member __.Delay f = f()
    
    member this.For(state: Volume , f: unit -> Volume) =
        let delayed = f()
        this.Combine(state, delayed)
    
    member this.Yield(name: string) = this.Name(this.Zero(), name)

    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        { state with name = name }

    [<CustomOperation "driver">]
    member _.Driver(state: Volume, driver: string) =   
        match state.volume with
        | CsiVolume v -> 
            let newVolume = CsiVolume { v with driver = driver }
            { state with volume = newVolume }
        | _ -> state
        
    [<CustomOperation "fsType">]
    member _.FsType(state: Volume, fsType: string) =   
        match state.volume with
        | CsiVolume v -> 
            let newVolume = CsiVolume { v with fsType = fsType }
            { state with volume = newVolume }
        | _ -> state
                
    [<CustomOperation "nodePublishSecretRef">]
    member _.NodePublishSecretRef (state: Volume, name: string) =   
        match state.volume with
        | CsiVolume v -> 
            let secretRef: LocalObjectReference = { name = Some name }
            let newVolume = CsiVolume { v with nodePublishSecretRef  = Some secretRef }
            { state with volume = newVolume }
        | _ -> state
        
    [<CustomOperation "readOnly">]
    member _.ReadOnly(state: Volume) =   
        match state.volume with
        | CsiVolume v -> 
            let newVolume = CsiVolume { v with readOnly = true }
            { state with volume = newVolume }
        | _ -> state

    member this.Yield(volumeAttributes: (string*string) list) = this.VolumeAttributes(this.Zero(), volumeAttributes)
    [<CustomOperation "volumeAttributes">]
    member _.VolumeAttributes(state: Volume, volumeAttributes: (string*string) list) = 
        match state.volume with
        | CsiVolume v -> 
            let newVolume = CsiVolume { v with volumeAttributes = volumeAttributes }
            { state with volume = newVolume }
        | _ -> state

// OPEN BUILDERS

[<AutoOpen>]
module VolumeBuilders =
    
    let persistentVolumeClaimVolume = new PersistentVolumeClaimVolumeBuilder()    
    let configMapVolume = new ConfigMapVolumeBuilder()
    let secretVolume = new SecretVolumeBuilder()
    let emptyDirVolume = new EmptyDirVolumeBuilder()
    let hostPathVolume = new HostPathVolumeBuilder()     
    let csiVolume = new CsiVolumeBuilder() 