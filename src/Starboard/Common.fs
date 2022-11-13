namespace Starboard.Common

[<AutoOpen>]
module Common =

    open Starboard

    type Metadata = 
        {
            name: string option
            generateName : string option
            ns: string option
            labels: (string*string) list
            annotations: (string*string) list }

    type Metadata with
        static member empty = {
            name = None
            generateName = None
            ns = Some "default"
            labels = List.empty
            annotations = List.empty 
        }
        static member combine metadata1 metadata2 =
            let mergeNamespace ns1 ns2 =
                match(ns1,ns2) with
                | ns1, Some "default" -> ns1
                | ns1, None -> ns1
                | Some "default", ns2 -> ns2
                | None, ns2 -> ns2
                | _ -> ns1
            {
                name = Helpers.mergeOption metadata1.name metadata2.name
                generateName = Helpers.mergeOption metadata1.generateName metadata2.generateName
                ns = mergeNamespace metadata1.ns metadata2.ns
                labels = List.append (metadata1.labels) (metadata2.labels)
                annotations = List.append (metadata1.annotations) (metadata2.annotations)
            }

        static member setName metadata name =
            { metadata with name = name }

        static member setNamespace metadata nsName =
            { metadata with ns = nsName }

        static member ToK8sModel metadata =
            {|
                name = metadata.name
                ``namespace`` = metadata.ns
                labels = Helpers.listToDict metadata.labels
                annotations = Helpers.listToDict metadata.annotations
            |}
        member this.Validate(kind: string) =
            (this |> Validation.required (fun m -> m.name) $"{kind} 'metadata.name' is required.")
            @ (this |> Validation.notEmpty (fun m -> m.name) $"{kind} 'metadata.name' cannot be empty.")
            @ (this |> Validation.required (fun m -> m.ns) $"{kind} 'metadata.namespace' is required.")
            @ (this |> Validation.notEmpty (fun m -> m.ns) $"{kind} 'metadata.namespace' cannot be empty.")

    
    type MetadataBuilder() =
        member _.Yield _ = Metadata.empty
    
        [<CustomOperation "name">]
        member _.Name(state: Metadata, name: string) = { state with name = Some name }
        
        [<CustomOperation "generateName">]
        member _.GenerateName(state: Metadata, generateName: string) = { state with generateName = Some generateName }
        
        [<CustomOperation "ns">]
        member _.Namespace(state: Metadata, ns: string) = { state with ns = Some ns }      
        
        [<CustomOperation "labels">]
        member _.Labels(state: Metadata, labels: (string*string) list) = { state with labels = labels }      
        
        [<CustomOperation "annotations">]
        member _.Annotations(state: Metadata, annotations: (string*string) list) = { state with annotations = annotations }      

    let metadata = MetadataBuilder()
        
    type MatchExpressionOperator = | In | NotIn | Exists | DoesNotExist 
        with override this.ToString() =
                match this with
                | In -> "In"
                | NotIn -> "NotIn"
                | Exists -> "Exists"
                | DoesNotExist -> "DoesNotExist"

    type LabelSelectorRequirement = { key: string; operator: MatchExpressionOperator; values: string list}
    type LabelSelectorRequirement with
        static member fromMatchLabel (key,value) = { key = key; operator = In; values = [value] }
        static member ToK8sModel (labelSelectorRequirement: LabelSelectorRequirement) =
            {|
                key = labelSelectorRequirement.key
                operator = labelSelectorRequirement.operator.ToString()
                values = labelSelectorRequirement.values
            |}

    //-------------------------
    // LabelSelector
    //-------------------------
    
    type LabelSelector = { matchExpressions: LabelSelectorRequirement list
                           matchLabels: (string*string) list }
    type LabelSelector with
        static member empty =
            { matchExpressions = List.empty
              matchLabels = List.empty }
            
        static member combine x1 x2 =
            if x1 = LabelSelector.empty then x2
            else x1

        static member ToK8sModel (labelSelector: LabelSelector) =
            let matchLabels = labelSelector.matchLabels
            let matchExpressions = labelSelector.matchExpressions

            let mapToMatchLabels lst = dict lst
            let mapToMatchExpressions labelSelectors =
                labelSelectors
                |> List.map LabelSelectorRequirement.ToK8sModel

            match (matchLabels, matchExpressions) with
            | [], [] -> None
            | lbls, exprs -> 
                {|
                    matchLabels = Helpers.mapValues mapToMatchLabels lbls
                    matchExpressions = Helpers.mapValues mapToMatchExpressions exprs
                |} |> Some
        member this.Spec() = LabelSelector.ToK8sModel this
        member this.Validate() = 
            let kind = "LabelSelector"
            Validation.requiredOneOfTwoLists (fun x -> x.matchExpressions) (fun y -> y.matchLabels) $"{kind} requires at least one of `matchLabels` or `matchExpressions`" this

    type LabelSelectorBuilder() =
        member _.Yield _ = LabelSelector.empty

        [<CustomOperation "matchLabel">]
        member _.MatchLabel(state: LabelSelector, (key,value): (string*string)) = 
            { state with matchLabels = List.append state.matchLabels [(key,value)] }

        [<CustomOperation "matchLabels">]
        member _.MatchLabel(state: LabelSelector, labels: (string*string) list) = 
            { state with matchLabels = List.append state.matchLabels labels }
 
        [<CustomOperation "matchDoesNotExist">]
        member _.MatchDoesNotExistExpression(state: LabelSelector, (key,values)) = 
            { state with matchExpressions = List.append state.matchExpressions [{ key = key; operator = MatchExpressionOperator.DoesNotExist; values = values }] }

        [<CustomOperation "matchExists">]
        member _.MatchExistsExpression(state: LabelSelector, (key,values)) = 
            { state with matchExpressions = List.append state.matchExpressions [{ key = key; operator = MatchExpressionOperator.Exists; values = values }] }
  
        [<CustomOperation "matchIn">]
        member _.MatchInExpression(state: LabelSelector, (key,values)) = 
            { state with matchExpressions = List.append state.matchExpressions [{ key = key; operator = MatchExpressionOperator.In; values = values }] }
    
        [<CustomOperation "matchNotIn">]
        member _.MatchNotInExpression(state: LabelSelector, (key,values)) = 
            { state with matchExpressions = List.append state.matchExpressions [{ key = key; operator = MatchExpressionOperator.NotIn; values = values }] }

    /// A label selector is a label query over a set of resources.
    /// https://kubernetes.io/docs/reference/kubernetes-api/common-definitions/label-selector/#LabelSelector
    let labelSelector = new LabelSelectorBuilder()

    //-------------------------
    // Container
    //-------------------------
    
    type Protocol = 
    | TCP | UDP | SCTP
    with override this.ToString() =
            match this with
            | TCP -> "TCP"
            | UDP -> "UDP"
            | SCTP -> "SCTP"
    type Protocol with
        static member combine p1 p2 =
            match (p1,p2) with
            | p1, TCP -> p1
            | TCP, p2 -> p2
            | _ -> p1
 
    type ContainerPort = {
        containerPort: int option
        hostIP: string option
        hostPort: int option
        name: string option
        protocol: Protocol
    }

    type ContainerPort with
        static member empty = 
            {
                containerPort = None
                hostIP = None
                hostPort = None
                name = None
                protocol = TCP
            }

        member this.Spec() =
            {|
                containerPort = this.containerPort
                hostIP = this.hostIP
                hostPort = this.hostPort
                name = this.name
                protocol = this.protocol.ToString()
            |}
        member this.Validate() =
            let kind = "ContainerPort"
            Validation.required (fun x -> x.containerPort) $"{kind} `containerPort` is required." this

    /// Millicpus: 1000m = 1cpu
    [<Measure>] type m
    /// Mebibytes 
    [<Measure>] type Mi

    type Resources = {
        memoryRequest: int<Mi>
        memoryLimit: int<Mi>
        cpuRequest: int<m>
        cpuLimit: int<m>
        //hugePages: (string * int<Mi>) list
    }
    type Resources with
        static member empty =
            {
                cpuLimit = 1000<m>
                memoryLimit = 512<Mi>
                cpuRequest = 500<m>
                memoryRequest = 256<Mi>
                //hugePages = List.empty
            }
        static member combine x1 x2 =
            if x1 = Resources.empty then x2
            else x1

        member this.Spec() =
            //TODO: return dictionary of string*obj to handle variable hugepage key 
            {|
                limits = {|
                    cpu = $"{this.cpuLimit}m"
                    memory = $"{this.memoryLimit}Mi"
                |}
                requests = {|
                    cpu = $"{this.cpuRequest}m"
                    memory = $"{this.memoryRequest}Mi"
                |}
                //hugePages = List.empty
            |}

    
    type ContainerPortBuilder() =
        member _.Yield _ = ContainerPort.empty

        member __.Zero () = ContainerPort.empty
        
        member __.Combine (currentValueFromYield: ContainerPort, accumulatorFromDelay: ContainerPort) = 
            { currentValueFromYield with 
                containerPort = Helpers.mergeOption (currentValueFromYield.containerPort) (accumulatorFromDelay.containerPort)
                hostIP = Helpers.mergeOption (currentValueFromYield.hostIP) (accumulatorFromDelay.hostIP)
                hostPort = Helpers.mergeOption (currentValueFromYield.hostPort) (accumulatorFromDelay.hostPort)
                name = Helpers.mergeOption (currentValueFromYield.name) (accumulatorFromDelay.name)
                protocol = Protocol.combine currentValueFromYield.protocol accumulatorFromDelay.protocol
            }
        
        member __.Delay f = f()
        
        member this.For(state: ContainerPort , f: unit -> ContainerPort) =
            let delayed = f()
            this.Combine(state, delayed)
        

        [<CustomOperation "port">]
        member _.ContainerPort(state: ContainerPort, containerPort: int) = { state with containerPort = Some containerPort }
        
        [<CustomOperation "hostIP">]
        member _.HostIP(state: ContainerPort, hostIP: string) = { state with hostIP = Some hostIP }
        
        [<CustomOperation "hostPort">]
        member _.HostPort(state: ContainerPort, hostPort: int) = { state with hostPort = Some hostPort }
        
        // Name
        member this.Yield(name: string) = this.Name(ContainerPort.empty, name)
        [<CustomOperation "name">]
        member _.Name(state: ContainerPort, name: string) = { state with name = Some name }
        
        [<CustomOperation "protocol">]
        member _.Protocol(state: ContainerPort, protocol: Protocol) = { state with protocol = protocol }
        
    let containerPort = new ContainerPortBuilder()


    type VolumeMount = {
        name: string option
        mountPath: string option
        readOnly: bool
        subPath: string
        subPathExpr: string
    }
    type VolumeMount with
        static member empty =
            {
                name = None
                mountPath = None
                readOnly = false
                subPath = ""
                subPathExpr = ""
            }
        member this.Spec() = 
            {|
                mountPath = this.mountPath
                name = this.name
                readOnly = this.readOnly
                subPath = this.subPath
                subPathExpr = this.subPathExpr
            |}

    
    type VolumeMountBuilder() =
        member _.Yield _ = VolumeMount.empty

        [<CustomOperation "name">]
        member _.Name(state: VolumeMount, name: string) = { state with name = Some name }
        
        [<CustomOperation "mountPath">]
        member _.MountPath(state: VolumeMount, mountPath: string) = { state with mountPath = Some mountPath }
        
        [<CustomOperation "readOnly">]
        member _.ReadOnly(state: VolumeMount) = { state with readOnly = true }
        

    let volumeMount = new VolumeMountBuilder()

    type EnvVar = {
        name: string
        value: string
    }
    type EnvVar with
        static member empty =
            {
                name = ""
                value = ""
            }
        member this.Spec() =
            {|
                name = this.name
                value = this.value
            |}

    type EnvVarBuilder() =
        member _.Yield _ = EnvVar.empty
    
        [<CustomOperation "name">]
        member _.Name(state: EnvVar, name: string) = { state with name = name }
        
        [<CustomOperation "value">]
        member _.Value(state: EnvVar, value: string) = { state with value = value }
        
        
    
    let envVar = new EnvVarBuilder()

    type Container = { 
        name: string option
        image: string
        command: string list
        args: string list 
        env: EnvVar list
        workingDir: string option
        ports: ContainerPort list
        resources: Resources
        volumeMounts: VolumeMount list
        // TODO: lifecyce
        // TODO: env.valueFrom
        // TODO: envFrom
        // TODO: security context
        // TODO: debugging

    }

    type Container with
        static member empty =
            { name = None
              image = "alpine:latest"
              command = List.empty
              args = List.empty 
              env = List.Empty 
              workingDir = None 
              ports = List.empty 
              resources = Resources.empty 
              volumeMounts = List.empty }

        member this.Spec() =
            {|
                name = this.name
                image = this.image
                command = this.command |> Helpers.mapValues id
                args = this.args |> Helpers.mapValues id
                workingDir = this.workingDir
                ports = this.ports |> Helpers.mapEach (fun p -> p.Spec())
                resources = this.resources.Spec()
                volumeMounts = this.volumeMounts |> Helpers.mapEach ((fun v -> v.Spec()))
                env = this.env |> Helpers.mapEach ((fun env -> env.Spec()))
            |}
        member this.Validate() =
            let kind = "Container"
            Validation.notEmpty (fun x -> x.name) $"{kind} `name` is required." this


    type ContainerBuilder() =

        member _.Zero _ = Container.empty
        member _.Yield _ = Container.empty

        member __.Combine (currentValueFromYield: Container, accumulatorFromDelay: Container) = 
            // TODO: test combine 
            { currentValueFromYield with 
                name = Helpers.mergeOption (currentValueFromYield.name) (accumulatorFromDelay.name) 
                image = Helpers.mergeString (currentValueFromYield.image) (accumulatorFromDelay.image)
                args = List.append (currentValueFromYield.args) (accumulatorFromDelay.args)
                command = List.append (currentValueFromYield.command) (accumulatorFromDelay.command)
                workingDir = Helpers.mergeOption (currentValueFromYield.workingDir) (accumulatorFromDelay.workingDir)
                resources = Resources.combine (currentValueFromYield.resources) (accumulatorFromDelay.resources)
                ports = List.append (currentValueFromYield.ports) (accumulatorFromDelay.ports)
                volumeMounts = List.append (currentValueFromYield.volumeMounts) (accumulatorFromDelay.volumeMounts)
                env = List.append (currentValueFromYield.env) (accumulatorFromDelay.env)
                
            } 
                
        
        member __.Delay f = f()
        
        member this.For(state: Container , f: unit -> Container) =
            let delayed = f()
            this.Combine(state, delayed)        
        
        member this.Yield(name: string) = this.Name(Container.empty, name)

        [<CustomOperation "name">]
        member _.Name(state: Container, name: string) = { state with name = Some name }

        [<CustomOperation "image">]
        member _.Image(state: Container, image: string) = { state with image = image }

        [<CustomOperation "args">]
        member _.Args(state: Container, args: string list) = { state with args = args }

        [<CustomOperation "command">]
        member _.Command(state: Container, command: string list) = { state with command = command }

        [<CustomOperation "workingDir">]
        member _.WorkingDir(state: Container, dir: string) = { state with workingDir = Some dir }

        // ContainerPort
        member this.Yield(containerPort: ContainerPort) = this.ContainerPort(Container.empty, containerPort)
        member this.Yield(containerPort: ContainerPort seq) = containerPort |> Seq.fold (fun state x -> this.ContainerPort(state, x)) Container.empty
        member this.YieldFrom(containerPort: ContainerPort seq) = this.Yield(containerPort)
        [<CustomOperation "add_port">]
        member _.ContainerPort(state: Container, port: ContainerPort) = { state with ports = List.append state.ports [port] }
        
        [<CustomOperation "cpuLimit">]
        member _.CpuLimit(state: Container, cpuLimit: int<m>) = 
            let newResources = { state.resources with cpuLimit = cpuLimit }
            { state with resources = newResources }
        
        [<CustomOperation "memoryLimit">]
        member _.MemoryLimit(state: Container, memoryLimit: int<Mi>) = 
            let newResources = { state.resources with memoryLimit = memoryLimit }
            { state with resources = newResources }

        [<CustomOperation "cpuRequest">]
        member _.CpuRequest(state: Container, cpuRequest: int<m>) = 
            let newResources = { state.resources with cpuRequest = cpuRequest }
            { state with resources = newResources }
        
        [<CustomOperation "memoryRequest">]
        member _.MemoryRequest(state: Container, memoryRequest: int<Mi>) = 
            let newResources = { state.resources with memoryRequest = memoryRequest }
            { state with resources = newResources }

        // VolumeMount
        member this.Yield(volumeMount: VolumeMount) = this.VolumeMount(Container.empty, volumeMount)
        member this.Yield(volumeMount: VolumeMount seq) = volumeMount |> Seq.fold (fun state x -> this.VolumeMount(state, x)) Container.empty
        member this.YieldFrom(volumeMount: VolumeMount seq) = this.Yield(volumeMount)
        [<CustomOperation "add_volumeMount">]
        member _.VolumeMount(state: Container, volumeMount: VolumeMount) = { state with volumeMounts = List.append state.volumeMounts [volumeMount] }
        
        // EnvVar
        member this.Yield(envVar: EnvVar) = this.AddEnvVar(Container.empty, envVar)
        member this.Yield(envVars: EnvVar seq) = envVars |> Seq.fold (fun state x -> this.AddEnvVar(state, x)) Container.empty
        member this.YieldFrom(envVars: EnvVar seq) = this.Yield(envVars)
        [<CustomOperation "add_envVar">]
        member _.AddEnvVar(state: Container, envVar: EnvVar) = { state with env = List.append state.env [envVar] }
        
        

    type LocalObjectReference = {
        name: string option
    }
    type LocalObjectReference with
        static member empty = {
            name = None
        }
    type LocalObjectReferenceBuilder() =
        member _.Yield _ = LocalObjectReference.empty
    
        [<CustomOperation "name">]
        member _.Name(state: LocalObjectReference, name: string) = { state with name = Some name }
        
    type ObjectReference = {
        apiVersion: string option
        fieldPath: string option
        kind: string option
        name: string option
        ns: string option
        resourceVersion: string option
        uid: string option
    }

    type ObjectReference with
        static member empty = 
            {
                apiVersion = None
                fieldPath = None
                kind = None
                name = None
                ns = None
                resourceVersion = None
                uid = None
            }
        static member combine curr prev: ObjectReference = 
            curr
    
    type ObjectReferenceBuilder() =
        member _.Yield _ = ObjectReference.empty

        [<CustomOperation "apiVersion">]
        member _.ApiVersion(state: ObjectReference, apiVersion: string) = 
            { state with apiVersion = Some apiVersion }

        [<CustomOperation "fieldPath">]
        member _.FieldPath (state: ObjectReference, fieldPath : string) = 
            { state with fieldPath  = Some fieldPath  }

        [<CustomOperation "kind">]
        member _.Kind(state: ObjectReference, kind : string) = 
            { state with kind  = Some kind  }

        [<CustomOperation "ns">]
        member _.Namespace(state: ObjectReference, ns : string) = 
            { state with ns  = Some ns  }
            
        [<CustomOperation "resourceVersion">]
        member _.ResourceVersion(state: ObjectReference, resourceVersion : string) = 
            { state with resourceVersion  = Some resourceVersion  }
 
        [<CustomOperation "uid">]
        member _.Uid(state: ObjectReference, uid : string) = 
            { state with uid  = Some uid  }

    type TypedLocalObjectReference = {
        kind:string option
        name: string option
        apiGroup: string option
    }
    type TypedLocalObjectReference with
        static member empty = {
            kind = None
            name = None
            apiGroup = None
        }
    type TypedLocalObjectReferenceBuilder() =
        member _.Yield _ = TypedLocalObjectReference.empty
    
        [<CustomOperation "kind">]
        member _.Kind(state: TypedLocalObjectReference, kind: string) = { state with kind = Some kind }
        
        [<CustomOperation "name">]
        member _.Name(state: TypedLocalObjectReference, name: string) = { state with name = Some name }
        
        [<CustomOperation "apiGroup">]
        member _.ApiGroup(state: TypedLocalObjectReference, apiGroup: string) = { state with apiGroup = Some apiGroup }
     
    type ResourceList = {
        apiVersion: string
        kind: string
        items: obj list
    }
    type ResourceList with
        static member empty = {
            apiVersion = "v1"
            kind = "List"
            items = List.empty
        }
        static member init items = { (ResourceList.empty) with items = items }
        member this.ToResource() = {|
            apiVersion = this.apiVersion
            kind = this.kind
            items = this.items
        |}

    type ResourceList<'a> = {
        apiVersion: string
        kind: string
        items: 'a list
    }
    type ResourceList<'a> with
        static member empty = {
            apiVersion = "v1"
            kind = "List"
            items = List.empty<'a>
        }
        static member init<'a> (items: 'a list) = 
            let name = $"{typedefof<'a>.Name}List"
            { (ResourceList<'a>.empty) with kind = name ; items = items }
        member this.ToResource(f) = {|
            apiVersion = this.apiVersion
            kind = this.kind
            items = this.items |> List.map f
        |}

    /// A single application container that you want to run within a pod.
    /// https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#Container

//====================================
// Builder init
//====================================
    let container = new ContainerBuilder()
    let localObjectRef = new LocalObjectReferenceBuilder()
    let objectReference = new ObjectReferenceBuilder()
    let typedLocalObjectReference = new TypedLocalObjectReferenceBuilder()
