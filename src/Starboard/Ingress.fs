namespace Starboard.Resources

open Starboard
//====================================
// IngressClass
// https://kubernetes.io/docs/reference/kubernetes-api/service-resources/ingress-class-v1/
//====================================

type IngressClassParametersScope = | Cluster | Namespace
type IngressClassParametersScope with
    member this.ToString() =
        match this with
        | Cluster -> "Cluster"
        | Namespace -> "Namespace"

type IngressClassParameters = {
    kind: string option
    name: string option
    apiGroup: string option
    ns: string option
    scope: IngressClassParametersScope
}

type IngressClassParameters with
    static member empty = {
        kind = None
        name = None
        apiGroup = None
        ns = None
        scope = IngressClassParametersScope.Cluster
    }

    member this.Spec() = {|
        kind = this.kind
        name = this.name
        apiGroup = this.apiGroup
        ``namespace`` = this.ns
        scope = this.scope.ToString()
    |}

type IngressClassParametersBuilder() =
    member _.Yield _ = IngressClassParameters.empty

    [<CustomOperation "kind">]
    member _.Kind(state: IngressClassParameters, kind: string) = { state with kind = Some kind }

    [<CustomOperation "name">]
    member _.Name(state: IngressClassParameters, name: string) = { state with name = Some name }

    [<CustomOperation "apiGroup">]
    member _.ApiGroup(state: IngressClassParameters, apiGroup: string) = { state with apiGroup = Some apiGroup }

    [<CustomOperation "ns">]
    member _.Ns(state: IngressClassParameters, ns: string) = { state with ns = Some ns }

    [<CustomOperation "scope">]
    member _.Scope(state: IngressClassParameters, scope: IngressClassParametersScope) = { state with scope = scope }

type IngressClass = {
    metadata: Metadata
    controller: string option
    parameters: IngressClassParameters option
}
type IngressClass with
    static member empty = {
        metadata = Metadata.empty
        controller = None
        parameters = None
    }
    member this.K8sVersion() = "networking.k8s.io/v1"
    member this.K8sKind() = "IngressClass"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.Spec() = {|
        controller = this.controller
        parameters = this.parameters |> Option.map (fun x -> x.Spec()) 
    |}
    member this.Valdidate() = List.empty
    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            spec = this.Spec()
        |}

type IngressClassBuilder() =
    member _.Yield _ = IngressClass.empty

    /// Name of the IngressClass. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: IngressClass, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the IngressClass.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: IngressClass, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the IngressClass
    [<CustomOperation "labels">]
    member _.Labels(state: IngressClass, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the IngressClass
    [<CustomOperation "annotations">]
    member _.Annotations(state: IngressClass, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }

    [<CustomOperation "controller">]
    member _.Controller(state: IngressClass, controller: string) = { state with controller = Some controller }

    [<CustomOperation "parameters">]
    member _.Parameters(state: IngressClass, parameters: IngressClassParameters) = { state with parameters = Some parameters }

//====================================
// Ingress
// https://kubernetes.io/docs/reference/kubernetes-api/service-resources/ingress-v1/
//====================================   
type ServiceBackendPort = | Name of string | Number of int
type IngressBackend = {
    resource: TypedLocalObjectReference option
    serviceName: string option // TODO: What if we default to "" for required fields and then validation checks not empty? Then records capture requirement...
    servicePort: ServiceBackendPort option
}
type IngressBackend with
    static member empty = {
        resource = None
        serviceName = None
        servicePort = None
    }
    member this.Spec() = 
        let port (p: ServiceBackendPort) =
            match p with
            | Name s -> {| name = Some s; number = None |}
            | Number i -> {| name = None; number = Some i |}
        {|
            resource = this.resource
            service = 
                if (this.serviceName.IsSome) || (this.servicePort.IsSome) then
                    Some {|
                        name = this.serviceName
                        port = this.servicePort |> Option.map port
                    |}
                else None
        |}

type IngressBackendBuilder() =
    member _.Yield _ = IngressBackend.empty

    [<CustomOperation "resource">]
    member _.Resource(state: IngressBackend, resource: TypedLocalObjectReference) = { state with resource = Some resource }

    [<CustomOperation "serviceName">]
    member _.ServiceName(state: IngressBackend, serviceName: string) = { state with serviceName = Some serviceName }
    
    [<CustomOperation "servicePortName">]
    member _.ServicePortName(state: IngressBackend, servicePortName: string) = { state with servicePort = Some (Name servicePortName) }

    [<CustomOperation "servicePortNumber">]
    member _.ServicePortNumber(state: IngressBackend, servicePortNumber: int) = { state with servicePort = Some (Number servicePortNumber) }
    

type IngressPathType = | Exact | Prefix | ImplementationSpecific
type HTTPIngressPath = {
    backend: IngressBackend
    pathType:IngressPathType
    path: string option
}
type HTTPIngressPath with
    static member empty = {
        backend = IngressBackend.empty
        pathType = Exact
        path = None
    }
    member this.Spec() = {|
        backend = this.backend.Spec()
        pathType = this.pathType.ToString()
        path = this.path
    |}
    member this.Validate() =
        Validation.required (fun x -> x.backend) "Ingress `rules.http.paths.backend` is required." this
        @ Validation.required (fun x -> x.pathType) "Ingress `rules.http.paths.pathType` is required." this
        @ Validation.startsWith "/" (fun x -> x.path) "Ingress `rules.http.paths.path` must start with '/'." this

type HTTPIngressPathBuilder() =
    member _.Yield _ = HTTPIngressPath.empty

    [<CustomOperation "backend">]
    member _.Backend(state: HTTPIngressPath, backend: IngressBackend) = { state with backend = backend }
    
    [<CustomOperation "pathType">]
    member _.PathType(state: HTTPIngressPath, pathType: IngressPathType) = { state with pathType = pathType }
    
    [<CustomOperation "path">]
    member _.Path(state: HTTPIngressPath, path: string) = { state with path = Some path }
    

type IngressRule = {
    host: string option
    httpPaths: HTTPIngressPath list
}
type IngressRule with
    static member empty = {
        host = None
        httpPaths = List.empty
    }
    member this.Spec() =
        {|
            host = this.host
            http = {|
                paths = this.httpPaths |> Helpers.mapEach (fun p -> p.Spec())
            |}
        |}
    member this.Validate() =
        Validation.notEmpty (fun r -> r.httpPaths) "Ingress `rules.http.paths` is required." this
        @ (this.httpPaths |> List.map (fun h -> h.Validate()) |> List.concat)

type IngressRuleBuilder() =
    member _.Yield _ = IngressRule.empty

    [<CustomOperation "host">]
    member _.Host(state: IngressRule, host: string) = { state with host = Some host }
    
    [<CustomOperation "httpPaths">]
    member _.HttpPaths(state: IngressRule, httpPaths: HTTPIngressPath list) = { state with httpPaths =  httpPaths }
    

type IngressTLS = {
    hosts: string list
    secretName: string option
}
type IngressTLS with
    static member empty = {
        hosts = List.empty
        secretName = None
    }

    member this.Spec() =
        {|
            hosts = this.hosts |> Helpers.mapEach id
            secretName = this.secretName
        |}

type IngressTLSBuilder() =
    member _.Yield _ = IngressTLS.empty

    [<CustomOperation "hosts">]
    member _.Hosts(state: IngressTLS, hosts: string list) = { state with hosts = hosts }
    
    [<CustomOperation "secretName">]
    member _.SecretName(state: IngressTLS, secretName: string) = { state with secretName = Some secretName }
    
type Ingress = {
    metadata: Metadata
    defaultBackend: IngressBackend option
    ingressClassName: string option
    rules: IngressRule list
    tls: IngressTLS list
}
type Ingress with
    static member empty = {
        metadata = Metadata.empty
        defaultBackend = None
        ingressClassName = None
        rules = List.empty
        tls = List.empty
    }
    member this.K8sVersion() = "networking.k8s.io/v1"
    member this.K8sKind() = "Ingress"
    member this.K8sMetadata() = Metadata.ToK8sModel this.metadata
    member this.Spec() =
        // https://kubernetes.io/docs/reference/kubernetes-api/service-resources/ingress-v1/#IngressSpec
        {|
            defaultBackend = this.defaultBackend |> Option.map (fun be -> be.Spec())
            ingressClassName = this.ingressClassName
            rules = this.rules |> Helpers.mapEach (fun r -> r.Spec() )
            tls = this.tls |> Helpers.mapEach  (fun t -> t.Spec() )
        |}
    member this.Valdidate() =
        (this.metadata.Validate(this.K8sKind()))
        @ (Validation.requiredIfEmpty (fun ingress -> ingress.defaultBackend) (fun ingress -> ingress.rules) "Ingress `defaultBackend` is required if no `rules` are specified." this)
        @ (this.rules |> List.map (fun rule -> rule.Validate()) |> List.concat)

    member this.ToResource() =
        {|
            apiVersion = this.K8sVersion()
            kind = this.K8sKind()
            metadata = this.K8sMetadata()
            spec = this.Spec()
        |}

type IngressBuilder() =
    member _.Yield _ = Ingress.empty

    /// Name of the Ingress. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: Ingress, name: string) = 
        let newMetadata = { state.metadata with name = name }
        { state with metadata = newMetadata}
    
    /// Namespace of the Ingress.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: Ingress, ns: string) = 
        let newMetadata = { state.metadata with ns = ns }
        { state with metadata = newMetadata }
    
    /// Labels for the Ingress
    [<CustomOperation "labels">]
    member _.Labels(state: Ingress, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the Ingress
    [<CustomOperation "annotations">]
    member _.Annotations(state: Ingress, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }

    [<CustomOperation "defaultBackend">]
    member _.DefaultBackend(state: Ingress, defaultBackend: IngressBackend) = { state with defaultBackend = Some defaultBackend }

    [<CustomOperation "ingressClassName">]
    member _.IngressClassName(state: Ingress, ingressClassName: string) = { state with ingressClassName = Some ingressClassName }

    [<CustomOperation "rules">]
    member _.Rules(state: Ingress, rules: IngressRule list) = { state with rules = rules }
    
    [<CustomOperation "tls">]
    member _.Tls(state: Ingress, tls: IngressTLS list) = { state with tls = tls }
    
//====================================
// Builder init
//====================================

[<AutoOpen>]
module IngressBuilders =
    let ingressClassParameters = new IngressClassParametersBuilder()
    let ingressClass = new IngressClassBuilder()
    let ingressBackend = new IngressBackendBuilder()
    let httpPath = new HTTPIngressPathBuilder()
    let ingressRule = new IngressRuleBuilder()
    let ingressTLS = new IngressTLSBuilder()
    let ingress = new IngressBuilder()