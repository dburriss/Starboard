namespace Starboard.Resources

module K8s =

    open Starboard.Resources

    // TODO: Service
    // TODO: Volume, 
    // TODO: PersistentVolume: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-claim-v1/
    // TODO: PersistentVolumeClaim: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/persistent-volume-v1/
    // TODO: Secret: https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/secret-v1/
    // TODO: Jobs
    // TODO: ReplicaSet?
    // TODO: Argo?


    //-------------------------
    // K8s resource list
    //-------------------------
    
    type K8s = obj list

    type K8sBuilder() =
        member _.Yield _ = List.Empty

        [<CustomOperation "configMap">]
        member _.ConfigMap(state: K8s, configMap: ConfigMap) = 
            List.append state [box (configMap.ToResource())]

        [<CustomOperation "pod">]
        member _.Pod(state: K8s, pod: Pod) = 
            List.append state [box (pod.ToResource())]

        [<CustomOperation "deployment">]
        member _.Deployment(state: K8s, deployment: Deployment) = 
            List.append state [box (deployment.ToResource())]

        [<CustomOperation "service">]
        member _.Service(state: K8s, service: Service) = 
            List.append state [box (service.ToResource())]
 
        [<CustomOperation "storageClass">]
        member _.StorageClass(state: K8s, storageClass: StorageClass) = 
            List.append state [box (storageClass.ToResource())]
        
        [<CustomOperation "persistentVolumeClaim">]
        member _.PersistentVolumeClaim(state: K8s, persistentVolumeClaim: PersistentVolumeClaim) = 
            List.append state [box (persistentVolumeClaim.ToResource())]
        
        [<CustomOperation "resource">]
        member _.Resource(state: K8s, resource: obj) = List.append state [resource]

        [<CustomOperation "resources">]
        member _.Resources(state: K8s, resources: obj list) = List.append state (resources |> List.map box)


    let k8s = new K8sBuilder()

    module KubeCtlWriter =
        open System
        open System.Text
        open Starboard.Serialization

        let toJson (k8s: K8s) =
            let list = {|
                apiVersion = "v1"
                kind = "List"
                items = k8s
            |}
            Serializer.toJson list


        let toJsonFile k8s filePath =
            let json = toJson k8s
            IO.File.WriteAllText(filePath, json)

        let toYaml (k8s: K8s) =

            if k8s.IsEmpty then String.Empty
            else
                let yamlDocuments = StringBuilder().AppendLine(k8s.Head |> Serializer.toYaml)
                for resource in k8s.Tail do
                    yamlDocuments
                        .AppendLine("---")
                        .AppendLine(resource |> Serializer.toYaml) |> ignore
                yamlDocuments.ToString()

        let toYamlFile k8s filePath =
            let json = toYaml k8s
            IO.File.WriteAllText(filePath, json)
        
        let print = toYaml >> printfn "%s" 
    
    