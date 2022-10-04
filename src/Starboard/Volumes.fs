namespace Starboard.Resources

open Starboard.Resources

// Volumes
// TODO: local
// TODO: secrets

// TODO: PersistentVolume
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
    
type HostPathType = | Default | DirectoryOrCreate | Directory | FileOrCreate | File | Socket | CharDevice | BlockDevice

/// hostPath represents a directory on the host
/// See: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-v1/#local
type HostPathType with
    member this.ToString() =
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


type Volume = { 
    configMap: ConfigMapVolumeSource option
    emptyDir: EmptyDirVolumeSource option
}

type Volume with
    static member empty =
        { 
            configMap = None
            emptyDir = None 
        }

    member this.Spec() =
        {|
            configMap = this.configMap
            emptyDir = this.emptyDir
        |}


type ConfigMapVolumeBuilder() =
    member _.Yield _ = Volume.empty

    [<CustomOperation "name">]
    member _.Name(state: Volume, name: string) = 
        let n =
            match state.configMap with
            | None -> { ConfigMapVolumeSource.empty with name = Some name }
            | Some x -> { x with name = Some name }
        { state with configMap = Some n }

    [<CustomOperation "optional">]
    member _.Optional(state: Volume) =
        let n =
            match state.configMap with
            | None -> { ConfigMapVolumeSource.empty with optional = true }
            | Some x -> { x with optional = true }
        { state with configMap = Some n }
   
    [<CustomOperation "defaultMode">]
    member _.DefaultMode(state: Volume, mode: int) =
        let n =
            match state.configMap with
            | None -> { ConfigMapVolumeSource.empty with defaultMode = Some mode }
            | Some x -> { x with defaultMode = Some mode }
        { state with configMap = Some n }
    
    [<CustomOperation "item">]
    member _.Item(state: Volume, keyValue) =
        let n =
            match state.configMap with
            | None -> { ConfigMapVolumeSource.empty with items = [KeyToPath.fromKeyValue(keyValue)] }
            | Some x -> { x with items = List.append x.items [KeyToPath.fromKeyValue(keyValue)] }
        { state with configMap = Some n }


type EmptyDirVolumeBuilder() =
    member _.Yield _ = Volume.empty

    [<CustomOperation "useMemory">]
    member _.UseMemory(state: Volume) =   
        let n =
            match state.emptyDir with
            | None -> { EmptyDirVolumeSource.empty with medium = "Memory" }
            | Some x -> { x with medium = "Memory" }
        { state with emptyDir = Some n }

    [<CustomOperation "sizeLimit">]
    member _.SizeLimit(state: Volume, size: int<Mi>) =   
        let n =
            match state.emptyDir with
            | None -> { EmptyDirVolumeSource.empty with sizeLimit = size }
            | Some x -> { x with sizeLimit = size }
        { state with emptyDir = Some n }


// OPEN BUILDERS

[<AutoOpen>]
module VolumeBuilders =
    
    let configMapVolume = new ConfigMapVolumeBuilder()
    let emptyDirVolume = new EmptyDirVolumeBuilder()    