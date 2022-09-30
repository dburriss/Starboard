namespace Starboard.Resources

open Starboard.Resources

// Volumes
// TODO: local
// TODO: secrets

// TODO: PersistentVolume
// TODO: PersistentVolumeClaim
// TODO: CSI

// STORAGE CLASS

// https://kubernetes.io/docs/concepts/storage/storage-classes/
type StorageClass = {
    metadata: Metadata
    provisioner: string option
    volumeBindingMode: string
    reclaimPolicy: string
    allowVolumeExpansion: bool
    parameters: (string*string) list
    mountOptions: string list
}
type StorageClass with
    static member empty =
        { 
            metadata = Metadata.empty
            provisioner = None
            volumeBindingMode = "Immediate"
            reclaimPolicy = "Delete"
            allowVolumeExpansion = false
            parameters = List.empty
            mountOptions = List.empty
            // TODO: allowedTopologies 
        }
    member _.K8sVersion() = "storage.k8s.io/v1"
    member _.K8sKind() = "StorageClass"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
    member this.ToResource() =
    
        // https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/storage-class-v1/
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            provisioner = this.provisioner
            volumeBindingMode = this.volumeBindingMode
            reclaimPolicy = this.reclaimPolicy
            allowVolumeExpansion = this.allowVolumeExpansion
            parameters = this.parameters |> Helpers.toDict
            mountOptions = this.mountOptions |> Helpers.mapValues id
        |}

type StorageClassBuilder() =
    member _.Yield _ = StorageClass.empty

    /// Name of the StorageClass. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: StorageClass, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}

    /// Namespace of the StorageClass.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: StorageClass, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Provisioner for CSI eg. blob.csi.azure.com
    /// See: https://kubernetes.io/docs/concepts/storage/storage-classes/#provisioner
    [<CustomOperation "provisioner">]
    member _.Provisioner(state: StorageClass, provisioner: string) = 
        { state with provisioner = Some provisioner }
    /// Controls when volume binding and provisioning occurs.
    /// Options are "Immediate" (default) or "WaitForFirstCustomer" (if supported).
    /// See: https://kubernetes.io/docs/concepts/storage/storage-classes/#volume-binding-mode
    [<CustomOperation "volumeBindingMode">]
    member _.VolumeBindingMode(state: StorageClass, volumeBindingMode: string) = 
        { state with volumeBindingMode = volumeBindingMode }
    
    /// Reclaim policy for the PersistentVolume created with this StorageClass. 
    /// Options are "Delete" (default) or "Reclaim".
    [<CustomOperation "reclaimPolicy">]
    member _.ReclaimPolicy(state: StorageClass, reclaimPolicy: string) = 
        { state with reclaimPolicy = reclaimPolicy }
    /// PersistentVolumes can be configured to be expandable. 
    /// This feature when set to true, allows the users to resize the volume by editing the corresponding PVC object.
    [<CustomOperation "allowVolumeExpansion">]
    member _.AllowVolumeExpansion(state: StorageClass) = 
        { state with allowVolumeExpansion = true }
        
    [<CustomOperation "parameters">]
    member _.Parameters(state: StorageClass, parameters: (string*string) list) = 
        { state with parameters = parameters }
         
    [<CustomOperation "mountOptions">]
    member _.MountOptions(state: StorageClass, mountOptions: string list) = 
        { state with mountOptions = mountOptions }
   


// VOLUMES

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
    
    let storageClass = new StorageClassBuilder()

    let configMapVolume = new ConfigMapVolumeBuilder()
    let emptyDirVolume = new EmptyDirVolumeBuilder()    