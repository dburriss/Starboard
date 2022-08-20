namespace Starboard.Resources

[<AutoOpen>]
module Common =
    open System.Collections.Generic
    
    type K8sResource =
        abstract member JsonModel : unit -> obj

    type Metadata = {
        name: string option
        generateName : string option
        ``namespace``: string option
        labels: IReadOnlyDictionary<string,string> option
        annotations: IReadOnlyDictionary<string,string> option
    }

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

    //-------------------------
    // LabelSelector
    //-------------------------
    
    type LabelSelector = { matchExpressions: LabelSelectorRequirement list
                           matchLabels: (string*string) list }
    type LabelSelector with
        static member empty =
            { matchExpressions = List.empty
              matchLabels = List.empty }

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
    
    type Container =
        { name: string option
          image: string
          command: string list
          args: string list 
          env: (string*string) list }


    type Container with
        static member Empty =
            { name = None
              image = "alpine:latest"
              command = List.empty
              args = List.empty 
              env = List.Empty }

    type ContainerBuilder() =
        member _.Yield _ = Container.Empty

        [<CustomOperation "name">]
        member _.Name(state: Container, name: string) = { state with name = Some name }

        [<CustomOperation "image">]
        member _.Image(state: Container, image: string) = { state with image = image }

        [<CustomOperation "args">]
        member _.Args(state: Container, args: string list) = { state with args = args }

        [<CustomOperation "command">]
        member _.Command(state: Container, command: string list) = { state with command = command }

    /// A single application container that you want to run within a pod.
    /// https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#Container
    let container = new ContainerBuilder()