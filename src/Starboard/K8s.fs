namespace Starboard.Resources

module K8s =

    open Starboard.Resources.Pods
    let pod = new PodBuilder()

    // TODO: ReplicaSet?
    // TODO: Volume, PersistentVolume
    // TODO: Service
    // TODO: Jobs
    // TODO: ConfigMap
    // TODO: Secret

    open Starboard.Resources.Deployments
    let deployment = new DeploymentBuilder()

    //-------------------------
    // K8s resource list
    //-------------------------
    
    type K8s = obj list

    //type K8s with
    //    static member Empty = List.empty

    type K8sBuilder() =
        member _.Yield _ = List.Empty

        [<CustomOperation "deployment">]
        member _.Deployment(state: K8s, deployment: Deployment) = 

            List.append state [box (deployment.ToResource())]
        

        [<CustomOperation "resource">]
        member _.Resource(state: K8s, resource: obj) = List.append state [resource]

        [<CustomOperation "resources">]
        member _.Resources(state: K8s, resources: obj list) = List.append state (resources |> List.map box)


    let k8s = new K8sBuilder()

    module KubeCtlWriter =
        open System
        open Starboard.Serialization

        let toJson (k8s: K8s) =
            let list = {|
                apiVersion = "v1"
                kind = "List"
                items = k8s
            |}
            Serializer.toJson list

        let print = toJson >> printfn "%s" 

        let toJsonFile k8s filePath =
            let json = toJson k8s
            IO.File.WriteAllText(filePath, json)
    
    