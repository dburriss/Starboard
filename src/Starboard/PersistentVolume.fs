namespace Starboard.Resources

type ReclaimPolicy = | Delete | Retain

type ReclaimPolicy with
    member this.ToString() =
        match this with
        | Delete -> "Delete"
        | Retain -> "Retain"

// STORAGE CLASS

// https://kubernetes.io/docs/concepts/storage/storage-classes/
type StorageClass = {
    metadata: Metadata
    provisioner: string option
    volumeBindingMode: string
    reclaimPolicy: ReclaimPolicy
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
            reclaimPolicy = Delete
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
            reclaimPolicy = this.reclaimPolicy.ToString()
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
    member _.ReclaimPolicy(state: StorageClass, reclaimPolicy: ReclaimPolicy) = 
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
   
// PersistentVolumeClaim
// https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-v1/

type VolumeMode = | Filesystem | Block

type VolumeMode with
    member this.ToString() =
        match this with
        | Filesystem -> "Filesystem"
        | Block -> "Block"

type AccessMode = | ReadWriteOnce | ReadOnlyMany | ReadWriteMany | ReadWriteOncePod

type AccessMode with
    member this.ToString() =
        match this with
        | ReadWriteOnce -> "ReadWriteOnce"
        | ReadOnlyMany -> "ReadOnlyMany"
        | ReadWriteMany -> "ReadWriteMany"
        | ReadWriteOncePod -> "ReadWriteOncePod"

type PersistentVolumeClaim = {
    metadata: Metadata
    volumeName: string option
    storageClassName: string option
    volumeMode: VolumeMode
    accessModes: AccessMode list
    selector: LabelSelector
    resources: Resources
    volumeSpec : (string*obj) option
}

/// PersistentVolumeClaim is a user's request for and claim to a persistent volume
/// See: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-claim-v1/
type PersistentVolumeClaim with
    static member empty =
        { 
            metadata = Metadata.empty
            volumeName = None
            storageClassName = None
            volumeMode = Filesystem
            accessModes = List.empty
            selector = LabelSelector.empty
            resources = Resources.empty
            volumeSpec = None
        }
    member _.K8sVersion() = "v1"
    member _.K8sKind() = "PersistentVolumeClaim"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
    member this.Spec() =
        {|
            volumeName = this.volumeName
            storageClassName = this.storageClassName
            volumeMode = this.volumeMode.ToString()
            accessModes = this.accessModes |> Helpers.mapEach (fun a -> a.ToString())
            selector = this.selector.Spec()
            resources = this.resources.Spec()
        |}
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            spec = this.Spec()
        |}

type PersistentVolumeClaimBuilder() =
    member _.Yield _ = PersistentVolumeClaim.empty

    /// Name of the StorageClass. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: PersistentVolumeClaim, metaName: string) = 
        let newMetadata = { state.metadata with name = Some metaName }
        { state with metadata = newMetadata}

    /// volumeName is the binding reference to the PersistentVolume backing this claim
    [<CustomOperation "volumeName">]
    member _.VolumeName(state: PersistentVolumeClaim, volumeName: string) = 
        { state with volumeName = Some volumeName}

    /// storageClassName is the name of the StorageClass required by the claim
    [<CustomOperation "storageClassName">]
    member _.StorageClassName(state: PersistentVolumeClaim, storageClassName: string) = 
        { state with storageClassName = Some storageClassName}

    /// volumeMode defines what type of volume is required by the claim
    [<CustomOperation "volumeMode">]
    member _.VolumeMode(state: PersistentVolumeClaim, volumeMode: VolumeMode) = 
        { state with volumeMode = volumeMode}

    /// accessModes contains the desired access modes the volume should have
    [<CustomOperation "accessModes">]
    member _.AccessModes(state: PersistentVolumeClaim, accessModes: AccessMode list) = 
        { state with accessModes = accessModes}

    /// selector is a label query over volumes to consider for binding
    [<CustomOperation "selector">]
    member _.Selector(state: PersistentVolumeClaim, selector: LabelSelector) = 
        { state with selector = selector}

    /// resources represents the minimum resources the volume should have
    [<CustomOperation "resources">]
    member _.Resources(state: PersistentVolumeClaim, resources: Resources) = 
        { state with resources = resources}

/// PersistentVolume (PV) is a storage resource provisioned by an administrator. It is analogous to a node. 
/// See: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-v1/
/// More info: https://kubernetes.io/docs/concepts/storage/persistent-volumes
type PersistentVolume = {
    metadata: Metadata
    accessModes: AccessMode list
    capacity: int<Mi> option
    claimRef: ObjectReference option
    mountOptions: string list
    // TODO: nodeAffinity
    persistentVolumeReclaimPolicy: ReclaimPolicy option
    storageClassName: string option
    volumeMode: VolumeMode
}

type PersistentVolume with
    static member empty =
        { 
            metadata = Metadata.empty
            accessModes = List.empty
            capacity = None
            claimRef = None
            mountOptions = List.empty
            // TODO: nodeAffinity
            persistentVolumeReclaimPolicy = None
            storageClassName = None
            volumeMode = VolumeMode.Filesystem
        }
    member _.K8sVersion() = "v1"
    member _.K8sKind() = "PersistentVolume"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
    member this.Spec() =
        {|
            accessModes = this.accessModes |> Helpers.mapEach (fun a -> a.ToString())
            capacity = this.capacity |> Option.map (fun x -> $"{x}Mi")
            claimRef = this.claimRef
            storageClassName = this.storageClassName
            mountOptions = this.mountOptions |> Helpers.mapValues id
            persistentVolumeReclaimPolicy = this.persistentVolumeReclaimPolicy
            storageClassName = this.storageClassName
            volumeMode = this.volumeMode.ToString()
        |}
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            spec = this.Spec()
        |}

type PersistentVolumeBuilder() =
    member _.Yield _ = PersistentVolumeClaim.empty

    /// Name of the StorageClass. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: PersistentVolumeClaim, metaName: string) = 
        let newMetadata = { state.metadata with name = Some metaName }
        { state with metadata = newMetadata}

    /// volumeName is the binding reference to the PersistentVolume backing this claim
    [<CustomOperation "volumeName">]
    member _.VolumeName(state: PersistentVolumeClaim, volumeName: string) = 
        { state with volumeName = Some volumeName}

    /// storageClassName is the name of the StorageClass required by the claim
    [<CustomOperation "storageClassName">]
    member _.StorageClassName(state: PersistentVolumeClaim, storageClassName: string) = 
        { state with storageClassName = Some storageClassName}

    /// volumeMode defines what type of volume is required by the claim
    [<CustomOperation "volumeMode">]
    member _.VolumeMode(state: PersistentVolumeClaim, volumeMode: VolumeMode) = 
        { state with volumeMode = volumeMode}

    /// accessModes contains the desired access modes the volume should have
    [<CustomOperation "accessModes">]
    member _.AccessModes(state: PersistentVolumeClaim, accessModes: AccessMode list) = 
        { state with accessModes = accessModes}

    /// selector is a label query over volumes to consider for binding
    [<CustomOperation "selector">]
    member _.Selector(state: PersistentVolumeClaim, selector: LabelSelector) = 
        { state with selector = selector}

    /// resources represents the minimum resources the volume should have
    [<CustomOperation "resources">]
    member _.Resources(state: PersistentVolumeClaim, resources: Resources) = 
        { state with resources = resources}

// OPEN BUILDERS

[<AutoOpen>]
module PersistentVolumeBuilders =
    
    let storageClass = new StorageClassBuilder()
    let persistentVolumeClaim = PersistentVolumeClaimBuilder();
