namespace Starboard.Resources

module K8s =

    open Starboard
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
    
    type K8s = { 
        resources: obj list
        errors: ValidationProblem list
    }
    type K8s with
        static member empty = {
            resources = List.empty
            errors = List.empty
        }
        member this.AddResource resource =
            { this with resources = List.append this.resources [resource] }

        member this.AddResources resources =
            { this with resources = List.append this.resources resources }
            
        member this.AddErrors errors =
            { this with errors = List.append this.errors errors }

        member this.IsEmpty() =
            this.resources |> List.isEmpty

    type K8sBuilder() =
        member _.Yield _ = K8s.empty

        [<CustomOperation "configMap">]
        member _.ConfigMap(state: K8s, configMap: ConfigMap) = 
            state.AddResource (box (configMap.ToResource()))

        [<CustomOperation "pod">]
        member _.Pod(state: K8s, pod: Pod) = 
            state.AddResource (box (pod.ToResource()))

        [<CustomOperation "deployment">]
        member _.Deployment(state: K8s, deployment: Deployment) = 
            state.AddResource (box (deployment.ToResource()))

        [<CustomOperation "service">]
        member _.Service(state: K8s, service: Service) = 
            state.AddResource (box (service.ToResource()))
 
        [<CustomOperation "ingressClass">]
        member _.IngressClass(state: K8s, ingressClass: IngressClass) = 
            state.AddResource (box (ingressClass.ToResource()))
            |> fun s -> s.AddErrors(ingressClass.Valdidate())
  
        [<CustomOperation "ingress">]
        member _.Ingress(state: K8s, ingress: Ingress) = 
            state.AddResource (box (ingress.ToResource()))
            |> fun s -> s.AddErrors(ingress.Valdidate())
 
        [<CustomOperation "storageClass">]
        member _.StorageClass(state: K8s, storageClass: StorageClass) = 
            state.AddResource (box (storageClass.ToResource()))
        
        [<CustomOperation "persistentVolumeClaim">]
        member _.PersistentVolumeClaim(state: K8s, persistentVolumeClaim: PersistentVolumeClaim) = 
            state.AddResource (box (persistentVolumeClaim.ToResource()))
         
        [<CustomOperation "persistentVolume">]
        member _.PersistentVolume<'a>(state: K8s, persistentVolume: PersistentVolume<'a>) = 
            state.AddResource (box (persistentVolume.ToResource()))
        
        [<CustomOperation "resource">]
        member _.Resource(state: K8s, resource: obj) = state.AddResource resource

        [<CustomOperation "resources">]
        member _.Resources(state: K8s, resources: obj list) = state.AddResources (resources |> List.map box)


    let k8s = new K8sBuilder()

    type K8sOutput = {
        isValid: bool
        content: string
        errors: ValidationProblem list
    }

    module KubeCtlWriter =
        open System
        open System.Text
        open Starboard.Serialization

        let private mapErrorToMessage (errors: ValidationProblem list) = errors |> List.map (fun e -> e.Message) 

        let toJson (k8s: K8s) =
            let resourceList = {|
                apiVersion = "v1"
                kind = "List"
                items = k8s.resources
            |}
            {
                isValid = k8s.errors |> List.isEmpty
                content = Serializer.toJson resourceList
                errors = k8s.errors
            }

        let toJsonFile k8s filePath =
            let output = toJson k8s
            IO.File.WriteAllText(filePath, output.content)
            output.errors

        let toYaml (k8s: K8s) =
            let content = 
                if k8s.resources.IsEmpty then String.Empty
                else
                    let yamlDocuments = StringBuilder().AppendLine(k8s.resources.Head |> Serializer.toYaml)
                    for resource in k8s.resources.Tail do
                        yamlDocuments
                            .AppendLine("---")
                            .AppendLine(resource |> Serializer.toYaml) |> ignore
                    yamlDocuments.ToString()
            {
                isValid = k8s.errors |> List.isEmpty
                content = content
                errors = k8s.errors
            }
            
        let toYamlFile k8s filePath =
            let output = toYaml k8s
            IO.File.WriteAllText(filePath, output.content)
            output.errors
        
        let print k8s = 
            do k8s |> toYaml |> fun output -> printfn "%s" (output.content)
            let errs = mapErrorToMessage k8s.errors
            let stdErr = Console.Error
            for e in errs do
                stdErr.WriteLine(e)
    
    