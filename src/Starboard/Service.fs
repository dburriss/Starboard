namespace Starboard.Resources

open Starboard.Resources
open System.Text.Json.Nodes

type IntOrString = | I of int32 | S of string
type IntOrString with
    member this.Value =
        // TODO: Create a serializer for this.
        match this with
        | I i -> JsonValue.Create(i).ToJsonString()
        | S s -> JsonValue.Create(s).ToJsonString()

type ServicePort = {
    port: int option
    targetPort: IntOrString option
    protocol: Protocol
    name: string option
    nodePort: int option
    appProtocol : string option
}


type ServicePort with
    static member empty = 
        {
            port = None
            targetPort = None
            protocol = TCP
            name = None
            nodePort = None
            appProtocol = None
        }

    member this.Spec() =
        {|
            port = this.port
            targetPort = this.targetPort |> Option.map (fun t -> t.Value)
            protocol = this.protocol.ToString()
            name = this.name
            nodePort = this.nodePort
            appProtocol = this.appProtocol
        |}

type ServicePortBuilder() =
    member _.Yield _ = ServicePort.empty

    [<CustomOperation "port">]
    member _.Port(state: ServicePort, port: int) = { state with port = Some port }
        
    [<CustomOperation "targetPortString">]
    member _.TargetPortS(state: ServicePort, targetPort: string) = { state with targetPort = Some (S targetPort) }

    [<CustomOperation "targetPortInt">]
    member _.TargetPortI(state: ServicePort, targetPort: int) = { state with targetPort = Some (I targetPort) }
        
    [<CustomOperation "protocol">]
    member _.Protocol(state: ServicePort, protocol: Protocol) = { state with protocol = protocol }
        
    [<CustomOperation "name">]
    member _.Name(state: ServicePort, name: string) = { state with name = Some name }
        
    [<CustomOperation "nodePort">]
    member _.NodePort(state: ServicePort, nodePort: int) = { state with nodePort = Some nodePort }
         
    [<CustomOperation "appProtocol">]
    member _.AppProtocol(state: ServicePort, appProtocol: string) = { state with appProtocol = Some appProtocol }       

type ServiceType = | ClusterIP | ExternalName | NodePort | LoadBalancer
type ServiceType with
    member this.ToString() =
        match this with
        | ClusterIP -> "ClusterIP"
        | ExternalName -> "ExternalName"
        | NodePort -> "NodePort"
        | LoadBalancer -> "LoadBalancer"

type Service = { 
    metadata: Metadata
    selector: LabelSelector
    ports: ServicePort list
    ``type``: ServiceType
}

type Service with
    static member empty =
        { 
            metadata = Metadata.empty
            selector = LabelSelector.empty
            ports = List.empty
            ``type`` = ClusterIP
        }

// resource: version, kind, metadata, spec
// template: metadata, spec
    member this.K8sVersion() = "v1"
    member this.K8sKind() = "Service"
    member this.K8sMetadata() = 
        if this.metadata = Metadata.empty then None
        else this.metadata |> Metadata.ToK8sModel |> Some
    member this.Spec() =
        // https://kubernetes.io/docs/reference/kubernetes-api/service-resources/service-v1/#ServiceSpec
        {|
            selector = this.selector.Spec()
            ports = this.ports |> Helpers.mapEach (fun p -> p.Spec())
            ``type`` = this.``type``.ToString()
        |}
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            spec = this.Spec()
        |}
        

type ServiceBuilder() =
        
    member _.Yield (_) = Service.empty
        
    /// Name of the Service. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: Service, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}

    /// Namespace of the Service.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: Service, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
        
    /// Labels for the Service
    [<CustomOperation "labels">]
    member _.Labels(state: Service, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }

    /// Annotations for the Service
    [<CustomOperation "annotations">]
    member _.Annotations(state: Service, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
        
    /// Selector for the Service. Used for complex selections. Use `matchLabel(s)` for simple label matching.
    [<CustomOperation "selector">]
    member _.Selector(state: Service, selectors: LabelSelector) = { state with selector = selectors }

    /// Add a single label selector to the Service.
    [<CustomOperation "matchLabel">]
    member _.MatchLabel(state: Service, (key,value)) =
        { state with selector = { state.selector with matchLabels = List.append state.selector.matchLabels [(key,value)] } }

    /// Add multiple label selectors to the Service.
    [<CustomOperation "matchLabels">]
    member _.MatchLabels(state: Service, labels) =
        { state with selector = { state.selector with matchLabels = List.append state.selector.matchLabels labels } }
    
    [<CustomOperation "port">]
    member _.Port(state: Service, port: ServicePort) = { state with ports = List.append state.ports [port] }

    /// Type of the Service.
    [<CustomOperation "typeOf">]
    member _.Type(state: Service, typeof: ServiceType) = 
        { state with ``type`` = typeof }

// TODO: Specific builders per service type

[<AutoOpen>]
module ServicerBuilders =
    let servicePort = new ServicePortBuilder()
    let service = new ServiceBuilder()