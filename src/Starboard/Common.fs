namespace Starboard.Resources

module Helpers =
    open System

    let nullableOfOption = function
        | None -> new Nullable<_>()
        | Some x -> new Nullable<_>(x)

    let mapValues f = function
        | [] -> None
        | xs -> Some (f xs)

[<AutoOpen>]
module Common =
    
    type K8sResource =
        abstract member JsonModel : unit -> obj

    type Metadata = {
        name: string option
        generateName : string option
        ns: string option
        labels: (string*string) list
        annotations: (string*string) list
    }

    type Metadata with
        static member empty = {
            name = None
            generateName = None
            ns = Some "default"
            labels = List.empty
            annotations = List.empty 
        }

        static member ToK8sModel metadata =
            let toK8sMap lst = Helpers.mapValues dict lst

            {|
                name = metadata.name
                ``namespace`` = metadata.ns
                labels = (toK8sMap metadata.labels)
                annotations = toK8sMap metadata.annotations
            |}

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

    type LabelSelectorBuilder() =
        member _.Yield _ = LabelSelector.empty

        [<CustomOperation "matchLabel">]
        member _.MatchLabel(state: LabelSelector, (key,value): (string*string)) = 
            { state with matchLabels = List.append state.matchLabels [(key,value)] }
 
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
    let selector = new LabelSelectorBuilder()

    //-------------------------
    // Container
    //-------------------------
    
    type Protocol = TCP | UDP | SCTP
    type Protocol with
        member this.ToString() =
            match this with
            | TCP -> "TCP"
            | UDP -> "UDP"
            | SCTP -> "SCTP"

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


    type Container = { 
        name: string option
        image: string
        command: string list
        args: string list 
        env: (string*string) list
        workingDir: string option
        ports: ContainerPort list
        // TODO: resources
        // TODO: volumes
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
              ports = List.empty }

        member this.Spec() =
            let mapPort p = {|
                containerPort = p.containerPort
                hostIP = p.hostIP
                hostPort = p.hostPort
                name = p.name
                protocol = p.protocol.ToString()
            |}
            let mapPorts = List.map mapPort
            {|
                name = this.name
                image = this.image
                command = this.command |> Helpers.mapValues id
                args = this.args |> Helpers.mapValues id
                workingDir = this.workingDir
                ports = Helpers.mapValues mapPorts this.ports
            |}

    type ContainerBuilder() =
        member _.Yield _ = Container.empty

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

        [<CustomOperation "port">]
        member _.Port(state: Container, port: ContainerPort) = { state with ports = List.append state.ports [port] }


    type ContainerPortBuilder() =
        member _.Yield _ = ContainerPort.empty

        [<CustomOperation "containerPort">]
        member _.ContainerPort(state: ContainerPort, containerPort: int) = { state with containerPort = Some containerPort }
        
        [<CustomOperation "hostIP">]
        member _.HostIP(state: ContainerPort, hostIP: string) = { state with hostIP = Some hostIP }
        
        [<CustomOperation "hostPort">]
        member _.HostPort(state: ContainerPort, hostPort: int) = { state with hostPort = Some hostPort }
        
        [<CustomOperation "name">]
        member _.Name(state: ContainerPort, name: string) = { state with name = Some name }
        
        [<CustomOperation "protocol">]
        member _.Protocol(state: ContainerPort, protocol: Protocol) = { state with protocol = protocol }


    /// A single application container that you want to run within a pod.
    /// https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#Container
    let container = new ContainerBuilder()    
    let containerPort = new ContainerPortBuilder()