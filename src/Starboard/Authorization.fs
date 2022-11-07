namespace Starboard.Authorization

open Starboard
open Starboard.Common

type RoleRef = {
    apiGroup: string
    kind: string
    name: string
}
type RoleRef with
    static member empty = 
        {
            apiGroup = ""
            kind = ""
            name = ""
        }
    static member combine v1 v2: RoleRef =
        { v1 with 
            apiGroup = Helpers.mergeString (v1.apiGroup) (v2.apiGroup)
            kind = Helpers.mergeString (v1.kind) (v2.kind)
            name = Helpers.mergeString (v1.name) (v2.name)
        }

type RoleRefBuilder() =
    member _.Yield _ = RoleRef.empty

    member __.Zero () = RoleRef.empty
    
    member __.Combine (currentValueFromYield: RoleRef, accumulatorFromDelay: RoleRef) = 
        RoleRef.combine currentValueFromYield accumulatorFromDelay
    
    member __.Delay f = f()
    
    member this.For(state: RoleRef , f: unit -> RoleRef) =
        let delayed = f()
        this.Combine(state, delayed)
    

    [<CustomOperation "apiGroup">]
    member _.ApiGroup(state: RoleRef, apiGroup: string) = { state with apiGroup = apiGroup }
    
    [<CustomOperation "kind">]
    member _.Kind(state: RoleRef, kind: string) = { state with kind = kind }
    
    // Name
    member this.Yield(name: string) = this.Name(RoleRef.empty, name)
    [<CustomOperation "name">]
    member _.Name(state: RoleRef, name: string) = { state with name = name }
    

type Subject = {
    kind: string
    name: string
    apiGroup: string option
    ns: string option
}
type Subject with
    static member empty = 
        {
            kind = ""
            name = ""
            apiGroup = None
            ns = None
        }

type SubjectBuilder() =
    member _.Yield _ = Subject.empty

    member __.Zero () = Subject.empty
    
    member __.Combine (currentValueFromYield: Subject, accumulatorFromDelay: Subject) = 
        { currentValueFromYield with 
            kind = Helpers.mergeString (currentValueFromYield.kind) (accumulatorFromDelay.kind)
            name = Helpers.mergeString (currentValueFromYield.name) (accumulatorFromDelay.name)
            apiGroup = Helpers.mergeOption (currentValueFromYield.apiGroup) (accumulatorFromDelay.apiGroup)
            ns = Helpers.mergeOption (currentValueFromYield.ns) (accumulatorFromDelay.ns)
        }
    
    member __.Delay f = f()
    
    member this.For(state: Subject , f: unit -> Subject) =
        let delayed = f()
        this.Combine(state, delayed)
    

    [<CustomOperation "kind">]
    member _.Kind(state: Subject, kind: string) = { state with kind = kind }

    // Name
    member this.Yield(name: string) = this.Name(Subject.empty, name)
    [<CustomOperation "name">]
    member _.Name(state: Subject, name: string) = { state with name = name }

    [<CustomOperation "apiGroup">]
    member _.ApiGroup(state: Subject, apiGroup: string) = { state with apiGroup = Some apiGroup }

    [<CustomOperation "ns">]
    member _.Ns(state: Subject, ns: string) = { state with ns = Some ns }
    

type ClusterRoleBinding = {
    metadata: Metadata
    roleRef: RoleRef
    subjects: Subject list
}
type ClusterRoleBinding with
    static member empty = 
        {
            metadata = Metadata.empty
            roleRef = RoleRef.empty
            subjects = List.empty
        }
    member this.K8sVersion() = "rbac.authorization.k8s.io/v1"
    member this.K8sKind() = "ClusterRoleBinding"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata

    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            roleRef = this.roleRef
            subjects = this.subjects |> Helpers.mapEach id
        |}

type ClusterRoleBindingBuilder() =
    member _.Yield _ = ClusterRoleBinding.empty

    member __.Zero () = ClusterRoleBinding.empty
    
    member __.Combine (currentValueFromYield: ClusterRoleBinding, accumulatorFromDelay: ClusterRoleBinding) = 
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            roleRef = RoleRef.combine currentValueFromYield.roleRef accumulatorFromDelay.roleRef
            subjects = List.append currentValueFromYield.subjects accumulatorFromDelay.subjects
        }
    
    member __.Delay f = f()
    
    member this.For(state: ClusterRoleBinding , f: unit -> ClusterRoleBinding) =
        let delayed = f()
        this.Combine(state, delayed)

    // Metadata
    member this.Yield(name: string) = this.Name(ClusterRoleBinding.empty, name)
    
    /// Name of the ClusterRoleBinding. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: ClusterRoleBinding, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the ClusterRoleBinding.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: ClusterRoleBinding, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the ClusterRoleBinding
    [<CustomOperation "_labels">]
    member _.Labels(state: ClusterRoleBinding, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the ClusterRoleBinding
    [<CustomOperation "_annotations">]
    member _.Annotations(state: ClusterRoleBinding, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(ClusterRoleBinding.empty, metadata)
    /// Sets the ClusterRoleBinding metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: ClusterRoleBinding, metadata: Metadata) =
        { state with metadata = metadata }

    // RoleRef
    member this.Yield(roleRef: RoleRef) = this.RoleRef(ClusterRoleBinding.empty, roleRef)
    [<CustomOperation "set_roleRef">]
    member this.RoleRef(state: ClusterRoleBinding, roleRef: RoleRef) = 
        { state with roleRef = roleRef }
    
    // Subject
    member this.Yield(subject: Subject) = this.Subjects(ClusterRoleBinding.empty, [subject])
    member this.Yield(subjects: Subject list) = this.Subjects(ClusterRoleBinding.empty, subjects)
    member this.YieldFrom(subjects: Subject seq) = this.Subjects(ClusterRoleBinding.empty, List.ofSeq subjects)
    [<CustomOperation "subjects">]
    member _.Subjects(state: ClusterRoleBinding, subjects: Subject list) = { state with subjects = List.append state.subjects subjects }
    
// OPEN BUILDERS

[<AutoOpen>]
module AuthenticationBuilders =
    
    let roleRef = new RoleRefBuilder()
    let subject = new SubjectBuilder()
    let clusterRoleBinding = new ClusterRoleBindingBuilder()