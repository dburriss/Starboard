namespace Starboard.Storage

open Starboard
open Starboard.Common

type ReclaimPolicy = | Delete | Retain

type ReclaimPolicy with
    member this.ToString() =
        match this with
        | Delete -> "Delete"
        | Retain -> "Retain"

type SecretReference = {
    /// Name is unique within a namespace to reference a secret resource.
    name: string option
    /// Namespace defines the space within which the secret name must be unique.
    ns: string option
}

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
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
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
            parameters = this.parameters |> Helpers.listToDict
            mountOptions = this.mountOptions |> Helpers.mapValues id
        |}

type StorageClassBuilder() =
    member _.Yield _ = StorageClass.empty

    member __.Zero () = StorageClass.empty
    
    member __.Combine (currentValueFromYield: StorageClass, accumulatorFromDelay: StorageClass) = 
        let mergeReclaimPolicy v1 v2 =
            match (v1,v2) with
            | v1, Delete -> v1
            | Delete, v2 -> v2
            | _ -> v1
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata  accumulatorFromDelay.metadata
            provisioner = Helpers.mergeOption (currentValueFromYield.provisioner) (accumulatorFromDelay.provisioner)
            volumeBindingMode = Helpers.mergeString (currentValueFromYield.volumeBindingMode) (accumulatorFromDelay.volumeBindingMode)
            reclaimPolicy = mergeReclaimPolicy currentValueFromYield.reclaimPolicy accumulatorFromDelay.reclaimPolicy
            allowVolumeExpansion = Helpers.mergeBool (currentValueFromYield.allowVolumeExpansion) (accumulatorFromDelay.allowVolumeExpansion)
            parameters = List.append (currentValueFromYield.parameters) (accumulatorFromDelay.parameters)
            mountOptions = List.append (currentValueFromYield.mountOptions) (accumulatorFromDelay.mountOptions)            
        }
    
    member __.Delay f = f()
    
    member this.For(state: StorageClass , f: unit -> StorageClass) =
        let delayed = f()
        this.Combine(state, delayed)
    
    // Metadata
    member this.Yield(name: string) = this.Name(StorageClass.empty, name)
    
    /// Name of the StorageClass. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: StorageClass, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the StorageClass.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: StorageClass, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the StorageClass
    [<CustomOperation "_labels">]
    member _.Labels(state: StorageClass, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the StorageClass
    [<CustomOperation "_annotations">]
    member _.Annotations(state: StorageClass, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(StorageClass.empty, metadata)
    /// Sets the StorageClass metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: StorageClass, metadata: Metadata) =
        { state with metadata = metadata }

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
    
    // ReclaimPolicy
    member this.Yield(reclaimPolicy: ReclaimPolicy) = this.ReclaimPolicy(StorageClass.empty, reclaimPolicy)
    member this.Yield(reclaimPolicy: ReclaimPolicy seq) = reclaimPolicy |> Seq.fold (fun state x -> this.ReclaimPolicy(state, x)) StorageClass.empty
    member this.YieldFrom(reclaimPolicy: ReclaimPolicy seq) = this.Yield(reclaimPolicy)
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
    
    // Parameters
    member this.Yield(parameters: (string*string) seq) = this.Parameters(StorageClass.empty, parameters |> List.ofSeq)
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
        }
    member _.K8sVersion() = "v1"
    member _.K8sKind() = "PersistentVolumeClaim"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
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

    // Metadata
    member this.Yield(name: string) = this.Name(PersistentVolumeClaim.empty, name)
    
    /// Name of the PersistentVolumeClaim. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: PersistentVolumeClaim, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the PersistentVolumeClaim.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: PersistentVolumeClaim, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the PersistentVolumeClaim
    [<CustomOperation "_labels">]
    member _.Labels(state: PersistentVolumeClaim, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the PersistentVolumeClaim
    [<CustomOperation "_annotations">]
    member _.Annotations(state: PersistentVolumeClaim, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(PersistentVolumeClaim.empty, metadata)
    /// Sets the PersistentVolumeClaim metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: PersistentVolumeClaim, metadata: Metadata) =
        { state with metadata = metadata }
    
    
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
type PersistentVolume<'a> = {
    metadata: Metadata
    accessModes: AccessMode list
    capacity: int<Mi> option
    claimRef: ObjectReference option
    mountOptions: string list
    // TODO: nodeAffinity
    persistentVolumeReclaimPolicy: ReclaimPolicy option
    storageClassName: string option
    volumeMode: VolumeMode
    volumeSpec : (string*'a) option
}

type PersistentVolume<'a> with
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
            volumeSpec = Option<string*'a>.None
        }
    member _.K8sVersion() = "v1"
    member _.K8sKind() = "PersistentVolume"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.Spec() =
        let (volType,volTypeSpec) = this.volumeSpec |> Option.get

        let addIfValueSome (k, v) m =
            match v with
            | Some v -> Map.add k (box v) m 
            | None -> m

        let addIfNotEmpty (k, lst) m =
            if List.isEmpty lst then m
            else Map.add k (box lst) m

        Map.empty
        |> addIfNotEmpty ("accessModes", this.accessModes |> List.map (fun a -> a.ToString()))
        |> addIfValueSome ("capacity", this.capacity |> Option.map (fun x -> $"{x}Mi"))
        |> addIfValueSome ("claimRef", this.claimRef)
        |> addIfValueSome ("storageClassName", this.storageClassName)
        |> addIfNotEmpty ("mountOptions", this.mountOptions)
        |> addIfValueSome ("persistentVolumeReclaimPolicy", this.persistentVolumeReclaimPolicy)
        |> Map.add "volumeMode" (this.volumeMode.ToString())
        |> Map.add volType (volTypeSpec |> box)
        |> Map.toSeq
        |> dict
        
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            spec = this.Spec()
        |}

type CSIPersistentVolumeSource ={
    driver: string option
    volumeHandle: string option
    fsType: string option
    readOnly: bool
    volumeAttributes: (Map<string,string>) option
    controllerExpandSecretRef: SecretReference option
    controllerPublishSecretRef: SecretReference option
    nodeExpandSecretRef: SecretReference option
    nodePublishSecretRef: SecretReference option
    nodeStageSecretRef: SecretReference option
}

type CSIPersistentVolumeSource with
    static member empty = {
        driver = None
        volumeHandle = None
        fsType = None
        readOnly = false
        volumeAttributes = None
        controllerExpandSecretRef = None
        controllerPublishSecretRef = None
        nodeExpandSecretRef = None
        nodePublishSecretRef = None
        nodeStageSecretRef = None
    }
/// csi represents storage that is handled by an external CSI driver.
/// See: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-v1/
/// More info: https://kubernetes.io/docs/concepts/storage/persistent-volumes
type CsiPersistentVolumeBuilder() =
    let csiInit spec = spec |> Option.defaultValue ("csi", CSIPersistentVolumeSource.empty)

    // TODO: see what it feels like with inheritance
    member _.Yield _ = PersistentVolume<CSIPersistentVolumeSource>.empty

    // Metadata
    member this.Yield(name: string) = this.Name(PersistentVolume<CSIPersistentVolumeSource>.empty, name)
    
    /// Name of the PersistentVolume<CSIPersistentVolumeSource>. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: PersistentVolume<CSIPersistentVolumeSource>, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the PersistentVolume<CSIPersistentVolumeSource>.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: PersistentVolume<CSIPersistentVolumeSource>, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the PersistentVolume<CSIPersistentVolumeSource>
    [<CustomOperation "_labels">]
    member _.Labels(state: PersistentVolume<CSIPersistentVolumeSource>, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the PersistentVolume<CSIPersistentVolumeSource>
    [<CustomOperation "_annotations">]
    member _.Annotations(state: PersistentVolume<CSIPersistentVolumeSource>, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(PersistentVolume<CSIPersistentVolumeSource>.empty, metadata)
    /// Sets the PersistentVolume<CSIPersistentVolumeSource> metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: PersistentVolume<CSIPersistentVolumeSource>, metadata: Metadata) =
        { state with metadata = metadata }
    
    /// capacity is the description of the persistent volume's resources and capacity. 
    /// More info: https://kubernetes.io/docs/concepts/storage/persistent-volumes#capacity
    [<CustomOperation "capacity">]
    member _.Capacity(state: PersistentVolume<CSIPersistentVolumeSource>, capacity: int<Mi>) = 
        { state with capacity = Some capacity}
        
    /// claimRef is part of a bi-directional binding between PersistentVolume and PersistentVolumeClaim. 
    /// See: https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-reference/#ObjectReference
    [<CustomOperation "claimRef">]
    member _.ClaimRef(state: PersistentVolume<CSIPersistentVolumeSource>, claimRef: ObjectReference) = 
        { state with claimRef = Some claimRef}

    /// storageClassName is the name of StorageClass to which this persistent volume belongs.
    [<CustomOperation "storageClassName">]
    member _.StorageClassName(state: PersistentVolume<CSIPersistentVolumeSource>, storageClassName: string) = 
        { state with storageClassName = Some storageClassName}

    /// volumeMode defines if a volume is intended to be used with a formatted filesystem or to remain in raw block state. Default: Filesystem
    [<CustomOperation "volumeMode">]
    member _.VolumeMode(state: PersistentVolume<CSIPersistentVolumeSource>, volumeMode: VolumeMode) = 
        { state with volumeMode = volumeMode}

    /// accessModes contains the desired access modes the volume should have
    [<CustomOperation "accessModes">]
    member _.AccessModes(state: PersistentVolume<CSIPersistentVolumeSource>, accessModes: AccessMode list) = 
        { state with accessModes = accessModes}
   
    // custom methods start here

    /// driver is the name of the driver to use for this volume. Required.
    [<CustomOperation "driver">]
    member _.VolumeSpec(state: PersistentVolume<CSIPersistentVolumeSource>, driver) = 
        let (label,csi) = state.volumeSpec |> csiInit
        let newCsi = { csi with driver = Some driver }
        { state with volumeSpec = Some (label, newCsi) }
        
    /// volumeHandle is the unique volume name returned by the CSI volume plugin’s CreateVolume to refer to the volume on all subsequent calls. Required.
    [<CustomOperation "volumeHandle">]
    member _.VolumeHandle(state: PersistentVolume<CSIPersistentVolumeSource>, volumeHandle) = 
        let (label,csi) = state.volumeSpec |> csiInit
        let newCsi = { csi with volumeHandle = Some volumeHandle }
        { state with volumeSpec = Some (label, newCsi) }
     
    /// fsType to mount. Must be a filesystem type supported by the host operating system. Ex. "ext4", "xfs", "ntfs".
    [<CustomOperation "fsType">]
    member _.FsType(state: PersistentVolume<CSIPersistentVolumeSource>, fsType) = 
        let (label,csi) = state.volumeSpec |> csiInit
        let newCsi = { csi with fsType = Some fsType }
        { state with volumeSpec = Some (label, newCsi) }
        
    /// readOnly value to pass to ControllerPublishVolumeRequest. Defaults to false (read/write).
    [<CustomOperation "readOnly">]
    member _.ReadOnly(state: PersistentVolume<CSIPersistentVolumeSource>) = 
        let (label,csi) = state.volumeSpec |> csiInit
        let newCsi = { csi with readOnly = true }
        { state with volumeSpec = Some (label, newCsi) }
        
    /// readOnly value to pass to ControllerPublishVolumeRequest. Defaults to false (read/write).
    [<CustomOperation "volumeAttributes">]
    member _.VolumeAttributes(state: PersistentVolume<CSIPersistentVolumeSource>, volumeAttributes: (string*string) list) = 
        let (label,csi) = state.volumeSpec |> csiInit
        let newCsi = { csi with volumeAttributes = Some (volumeAttributes |> Map.ofList) }
        { state with volumeSpec = Some (label, newCsi) }

//====================================
// Builder init
//====================================

[<AutoOpen>]
module PersistentVolumeBuilders =
    
    let storageClass = new StorageClassBuilder()
    let persistentVolumeClaim = new PersistentVolumeClaimBuilder();
    let csi = new CsiPersistentVolumeBuilder()
