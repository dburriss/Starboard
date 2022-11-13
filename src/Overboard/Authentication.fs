namespace Overboard.Authentication

open Overboard
open Overboard.Common


type ServiceAccount = {
    metadata: Metadata
    automountServiceAccountToken: bool
    imagePullSecrets: LocalObjectReference list
    secrets: ObjectReference list
}
type ServiceAccount with
    static member empty = 
        {
            metadata = Metadata.empty
            automountServiceAccountToken = false // This differs from spec but is safer
            imagePullSecrets = List.empty
            secrets = List.empty
        }
    member this.K8sVersion() = "v1"
    member this.K8sKind() = "ServiceAccount"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata

    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            automountServiceAccountToken = this.automountServiceAccountToken
            imagePullSecrets = this.imagePullSecrets |> Helpers.mapEach id
            secrets = this.secrets |> Helpers.mapEach id
        |}

type ServiceAccountBuilder() =
    member _.Yield _ = ServiceAccount.empty

    member __.Zero () = ServiceAccount.empty
    
    member __.Combine (currentValueFromYield: ServiceAccount, accumulatorFromDelay: ServiceAccount) = 
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            automountServiceAccountToken = Helpers.mergeBool (currentValueFromYield.automountServiceAccountToken) (accumulatorFromDelay.automountServiceAccountToken)
            imagePullSecrets = List.append (currentValueFromYield.imagePullSecrets) (accumulatorFromDelay.imagePullSecrets)
            secrets = List.append currentValueFromYield.secrets accumulatorFromDelay.secrets
        }
    
    member __.Delay f = f()
    
    member this.For(state: ServiceAccount , f: unit -> ServiceAccount) =
        let delayed = f()
        this.Combine(state, delayed)

    // Metadata
    member this.Yield(name: string) = this.Name(ServiceAccount.empty, name)
    
    /// Name of the ServiceAccount. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: ServiceAccount, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the ServiceAccount.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: ServiceAccount, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the ServiceAccount
    [<CustomOperation "_labels">]
    member _.Labels(state: ServiceAccount, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the ServiceAccount
    [<CustomOperation "_annotations">]
    member _.Annotations(state: ServiceAccount, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(ServiceAccount.empty, metadata)
    /// Sets the ServiceAccount metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: ServiceAccount, metadata: Metadata) =
        { state with metadata = metadata }
    
    [<CustomOperation "automountServiceAccountToken">]
    member _.AutomountServiceAccountToken(state: ServiceAccount) = { state with automountServiceAccountToken = true }
    
    // ImagePullSecrets
    [<CustomOperation "imagePullSecrets">]
    member _.ImagePullSecrets(state: ServiceAccount, imagePullSecrets: string list) = 
        let mappedSecrets = imagePullSecrets |> List.map (fun s -> { name = Some s })
        { state with imagePullSecrets = mappedSecrets }
    
    // ObjectReference
    member this.Yield(secrets: ObjectReference list) = this.Secrets(ServiceAccount.empty, secrets)
    member this.YieldFrom(secrets: ObjectReference seq) = this.Secrets(ServiceAccount.empty, List.ofSeq secrets)
    [<CustomOperation "secrets">]
    member _.Secrets(state: ServiceAccount, secrets: ObjectReference list) = { state with secrets = List.append state.secrets secrets }
    
// OPEN BUILDERS

[<AutoOpen>]
module AuthenticationBuilders =
    
    let serviceAccount = new ServiceAccountBuilder()