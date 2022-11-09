namespace Starboard.Service

open Starboard
open Starboard.Common
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

    member __.Zero () = IngressClass.empty
    
    member __.Combine (currentValueFromYield: IngressClass, accumulatorFromDelay: IngressClass) = 
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            controller = Helpers.mergeOption (currentValueFromYield.controller) (accumulatorFromDelay.controller)
            parameters = Helpers.mergeOption (currentValueFromYield.parameters) (accumulatorFromDelay.parameters)
        }
    
    member __.Delay f = f()
    
    member this.For(state: IngressClass , f: unit -> IngressClass) =
        let delayed = f()
        this.Combine(state, delayed)
    

    /// Name of the IngressClass. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "name">]
    member _.Name(state: IngressClass, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the IngressClass.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "ns">]
    member _.Namespace(state: IngressClass, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
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

    // IngressClassParameters
    member this.Yield(ingressClassParameters: IngressClassParameters) = this.IngressClassParameters(IngressClass.empty, ingressClassParameters)
    [<CustomOperation "parameters">]
    member _.IngressClassParameters(state: IngressClass, parameters: IngressClassParameters) = { state with parameters = Some parameters }

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

    member __.Zero () = IngressBackend.empty
    
    member __.Combine (currentValueFromYield: IngressBackend, accumulatorFromDelay: IngressBackend) = 
        { currentValueFromYield with 
            resource = Helpers.mergeOption (currentValueFromYield.resource) (accumulatorFromDelay.resource)
            serviceName = Helpers.mergeOption (currentValueFromYield.serviceName) (accumulatorFromDelay.serviceName)
            servicePort = Helpers.mergeOption (currentValueFromYield.servicePort) (accumulatorFromDelay.servicePort)
        }
    
    member __.Delay f = f()
    
    member this.For(state: IngressBackend , f: unit -> IngressBackend) =
        let delayed = f()
        this.Combine(state, delayed)
    

    // TypedLocalObjectReference
    member this.Yield(typedLocalObjectReference: TypedLocalObjectReference) = this.Resource(IngressBackend.empty, typedLocalObjectReference)
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

    member __.Zero () = IngressRule.empty
    
    member __.Combine (currentValueFromYield: IngressRule, accumulatorFromDelay: IngressRule) = 
        { currentValueFromYield with 
            host = Helpers.mergeOption (currentValueFromYield.host) (accumulatorFromDelay.host)
            httpPaths = List.append (currentValueFromYield.httpPaths) (accumulatorFromDelay.httpPaths)
        }
    
    member __.Delay f = f()
    
    member this.For(state: IngressRule , f: unit -> IngressRule) =
        let delayed = f()
        this.Combine(state, delayed)
    

    [<CustomOperation "host">]
    member _.Host(state: IngressRule, host: string) = { state with host = Some host }
    
    // name
    member this.Yield(httpPaths: HTTPIngressPath seq) = this.HttpPaths(IngressRule.empty, httpPaths |> List.ofSeq)
    member this.YieldFrom(httpPaths: HTTPIngressPath seq) = this.Yield(httpPaths)
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

    member __.Zero () = Ingress.empty
    
    member __.Combine (currentValueFromYield: Ingress, accumulatorFromDelay: Ingress) = 
        { currentValueFromYield with 
            metadata = Metadata.combine currentValueFromYield.metadata accumulatorFromDelay.metadata
            defaultBackend = Helpers.mergeOption (currentValueFromYield.defaultBackend) (accumulatorFromDelay.defaultBackend)
            ingressClassName = Helpers.mergeOption (currentValueFromYield.ingressClassName) (accumulatorFromDelay.ingressClassName)
            rules = List.append (currentValueFromYield.rules) (accumulatorFromDelay.rules)
            tls = List.append (currentValueFromYield.tls) (accumulatorFromDelay.tls)
        }
    
    member __.Delay f = f()
    
    member this.For(state: Ingress , f: unit -> Ingress) =
        let delayed = f()
        this.Combine(state, delayed)
    
    // Metadata
    member this.Yield(name: string) = this.Name(Ingress.empty, name)
    
    /// Name of the Ingress. 
    /// Name must be unique within a namespace. 
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_name">]
    member _.Name(state: Ingress, name: string) = 
        let newMetadata = { state.metadata with name = Some name }
        { state with metadata = newMetadata}
    
    /// Namespace of the Ingress.
    /// Namespace defines the space within which each name must be unique. Default is "default".
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/object-meta/#ObjectMeta
    [<CustomOperation "_namespace">]
    member _.Namespace(state: Ingress, ns: string) = 
        let newMetadata = { state.metadata with ns = Some ns }
        { state with metadata = newMetadata }
    
    /// Labels for the Ingress
    [<CustomOperation "_labels">]
    member _.Labels(state: Ingress, labels: (string*string) list) = 
        let newMetadata = { state.metadata with labels = labels }
        { state with metadata = newMetadata }
    
    /// Annotations for the Ingress
    [<CustomOperation "_annotations">]
    member _.Annotations(state: Ingress, annotations: (string*string) list) = 
        let newMetadata = { state.metadata with annotations = annotations }
        { state with metadata = newMetadata }
    
    member this.Yield(metadata: Metadata) = this.SetMetadata(Ingress.empty, metadata)
    /// Sets the Ingress metadata
    [<CustomOperation "set_metadata">]
    member _.SetMetadata(state: Ingress, metadata: Metadata) =
        { state with metadata = metadata }
    
    // IngressBackend
    member this.Yield(ingressBackend: IngressBackend) = this.DefaultBackend(Ingress.empty, ingressBackend)
    [<CustomOperation "defaultBackend">]
    member _.DefaultBackend(state: Ingress, defaultBackend: IngressBackend) = { state with defaultBackend = Some defaultBackend }

    [<CustomOperation "ingressClassName">]
    member _.IngressClassName(state: Ingress, ingressClassName: string) = { state with ingressClassName = Some ingressClassName }

    // TODO: operation should replace and yields append
    // IngressRule
    member this.Yield(ingressRule: IngressRule) = this.AddRules(Ingress.empty, [ingressRule])
    member this.Yield(ingressRule: IngressRule seq) = ingressRule |> Seq.fold (fun state x -> this.AddRules(state, [x])) Ingress.empty
    member this.YieldFrom(ingressRule: IngressRule seq) = this.Yield(ingressRule)
    [<CustomOperation "add_rules">]
    member _.AddRules(state: Ingress, rules: IngressRule list) = { state with rules = List.append state.rules rules }
    
    /// Sets the ingress rules, overwriting any existing rules
    [<CustomOperation "set_rules">]
    member _.SetRules(state: Ingress, rules: IngressRule list) = { state with rules = rules }

    // IngressTls
    member this.Yield(ingressTls: IngressTLS) = this.AddTls(Ingress.empty, [ingressTls])
    member this.Yield(ingressTls: IngressTLS seq) = ingressTls |> Seq.fold (fun state x -> this.AddTls(state, [x])) Ingress.empty
    member this.YieldFrom(ingressTls: IngressTLS seq) = this.Yield(ingressTls)
    [<CustomOperation "add_tls">]
    member _.AddTls(state: Ingress, tls: IngressTLS list) = { state with tls = List.append state.tls tls }

    /// Sets the TLS, overwriting any existing TLS
    [<CustomOperation "set_tls">]
    member _.SetTls(state: Ingress, tls: IngressTLS list) = { state with tls = tls }
    
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