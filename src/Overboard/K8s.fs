namespace Overboard
[<AutoOpen>]
module K8s =

    open Overboard
    open Overboard.Storage
    open Overboard.Workload
    open Overboard.Service
    open Overboard.Authentication
    open Overboard.Authorization
    open Overboard.Cluster

    // TODO: Jobs
    // TODO: ReplicaSet?
    // TODO: Argo?
    // TODO: Dapper?


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

        member __.Zero(_) = K8s.empty
        member __.Yield(_) = K8s.empty

        member __.Combine (currentValueFromYield: K8s, accumulatorFromDelay: K8s) = 
            { currentValueFromYield with 
                resources = currentValueFromYield.resources @ accumulatorFromDelay.resources;
                errors = (currentValueFromYield.errors @ accumulatorFromDelay.errors) |> List.distinct
            }
        member __.Delay f = f()

        member this.For(state: K8s , f: unit -> K8s) =
            let delayed = f()
            this.Combine(state, delayed)


        // member _.Run(f) : MnM = 
        //     // run validation?
        //     f(K8s.empty)

        // Secret
        member this.Yield(secret: Secret) = this.Secret(K8s.empty, secret)
        member this.Yield(secrets: Secret seq) = secrets |> Seq.fold (fun state s -> this.Secret(state, s)) K8s.empty
        member this.YieldFrom(secrets: Secret seq) = this.Yield(secrets)
        [<CustomOperation "add_secret">]
        member __.Secret(state: K8s, secret: Secret) = 
            state.AddResource (box (secret.ToResource()))

        // SecretList
        member this.Yield(secretList: SecretList) = this.SecretList(K8s.empty, secretList)
        member this.Yield(secretList: SecretList seq) = secretList |> Seq.fold (fun state x -> this.SecretList(state, x)) K8s.empty
        member this.YieldFrom(secretList: SecretList seq) = this.Yield(secretList)
        [<CustomOperation "add_secretList">]
        member __.SecretList(state: K8s, secretList: SecretList) = 
            state.AddResource (box (secretList.ToResource(fun s -> s.ToResource() )))

        // ConfigMap
        member this.Yield(configMap: ConfigMap) = this.ConfigMap(K8s.empty, configMap)
        member this.Yield(configMap: ConfigMap seq) = configMap |> Seq.fold (fun state x -> this.ConfigMap(state, x)) K8s.empty
        member this.YieldFrom(configMap: ConfigMap seq) = this.Yield(configMap)
        [<CustomOperation "add_configMap">]
        member __.ConfigMap(state: K8s, configMap: ConfigMap) = 
            state.AddResource (box (configMap.ToResource()))
            
        // ConfigMapList
        member this.Yield(configMapList: ConfigMapList) = this.ConfigMapList(K8s.empty, configMapList)
        member this.Yield(configMapList: ConfigMapList seq) = configMapList |> Seq.fold (fun state x -> this.ConfigMapList(state, x)) K8s.empty
        member this.YieldFrom(configMapList: ConfigMapList seq) = this.Yield(configMapList)
        [<CustomOperation "add_configMapList">]
        member __.ConfigMapList(state: K8s, configMapList: ConfigMapList) = 
            state.AddResource (box (configMapList.ToResource(fun c -> c.ToResource() )))

        // Pod
        member this.Yield(pod: Pod) = this.Pod(K8s.empty, pod)
        member this.Yield(pod: Pod seq) = pod |> Seq.fold (fun state x -> this.Pod(state, x)) K8s.empty
        member this.YieldFrom(pod: Pod seq) = this.Yield(pod)
        [<CustomOperation "add_pod">]
        member __.Pod(state: K8s, pod: Pod) = 
            state.AddResource (box (pod.ToResource()))
            |> fun s -> s.AddErrors(pod.Validate())

        // Deployment
        member this.Yield(deployment: Deployment) = this.Deployment(K8s.empty, deployment)
        member this.Yield(deployments: Deployment seq) = deployments |> Seq.fold (fun state d -> this.Deployment(state, d)) K8s.empty
        member this.YieldFrom(deployments: Deployment seq) = this.Yield(deployments)
        [<CustomOperation "add_deployment">]
        member __.Deployment(state: K8s, deployment: Deployment) = 
            state.AddResource (box (deployment.ToResource()))
            |> fun s -> s.AddErrors(deployment.Validate())
            
        // Job
        member this.Yield(job: Job) = this.Job(K8s.empty, job)
        member this.Yield(jobs: Job seq) = jobs |> Seq.fold (fun state d -> this.Job(state, d)) K8s.empty
        member this.YieldFrom(jobs: Job seq) = this.Yield(jobs)
        [<CustomOperation "add_job">]
        member __.Job(state: K8s, job: Job) = 
            state.AddResource (box (job.ToResource()))
            |> fun s -> s.AddErrors(job.Validate())
            
        // CronJob
        member this.Yield(cronJob: CronJob) = this.CronJob(K8s.empty, cronJob)
        member this.Yield(cronJobs: CronJob seq) = cronJobs |> Seq.fold (fun state d -> this.CronJob(state, d)) K8s.empty
        member this.YieldFrom(cronJobs: CronJob seq) = this.Yield(cronJobs)
        [<CustomOperation "add_cronJob">]
        member __.CronJob(state: K8s, cronJob: CronJob) = 
            state.AddResource (box (cronJob.ToResource()))
            |> fun s -> s.AddErrors(cronJob.Validate())
            
        // Service
        member this.Yield(service: Service) = this.Service(K8s.empty, service)
        member this.Yield(service: Service seq) = service |> Seq.fold (fun state x -> this.Service(state, x)) K8s.empty
        member this.YieldFrom(service: Service seq) = this.Yield(service)
        [<CustomOperation "add_service">]
        member __.Service(state: K8s, service: Service) = 
            state.AddResource (box (service.ToResource()))
 
        // IngressClass
        member this.Yield(ingressClass: IngressClass) = this.IngressClass(K8s.empty, ingressClass)
        member this.Yield(ingressClass: IngressClass seq) = ingressClass |> Seq.fold (fun state x -> this.IngressClass(state, x)) K8s.empty
        member this.YieldFrom(ingressClass: IngressClass seq) = this.Yield(ingressClass)
        [<CustomOperation "add_ingressClass">]
        member __.IngressClass(state: K8s, ingressClass: IngressClass) = 
            state.AddResource (box (ingressClass.ToResource()))
            |> fun s -> s.AddErrors(ingressClass.Valdidate())
  
        // Ingress
        member this.Yield(ingress: Ingress) = this.Ingress(K8s.empty, ingress)
        member this.Yield(ingress: Ingress seq) = ingress |> Seq.fold (fun state x -> this.Ingress(state, x)) K8s.empty
        member this.YieldFrom(ingress: Ingress seq) = this.Yield(ingress)
        [<CustomOperation "add_ingress">]
        member __.Ingress(state: K8s, ingress: Ingress) = 
            state.AddResource (box (ingress.ToResource()))
            |> fun s -> s.AddErrors(ingress.Valdidate())
 
        // StorageClass
        member this.Yield(storageClass: StorageClass) = this.StorageClass(K8s.empty, storageClass)
        member this.Yield(storageClass: StorageClass seq) = storageClass |> Seq.fold (fun state x -> this.StorageClass(state, x)) K8s.empty
        member this.YieldFrom(storageClass: StorageClass seq) = this.Yield(storageClass)
        [<CustomOperation "add_storageClass">]
        member __.StorageClass(state: K8s, storageClass: StorageClass) = 
            state.AddResource (box (storageClass.ToResource()))
        
        // PersistentVolumeClaim
        member this.Yield(persistentVolumeClaim: PersistentVolumeClaim) = this.PersistentVolumeClaim(K8s.empty, persistentVolumeClaim)
        member this.Yield(persistentVolumeClaim: PersistentVolumeClaim seq) = persistentVolumeClaim |> Seq.fold (fun state x -> this.PersistentVolumeClaim(state, x)) K8s.empty
        member this.YieldFrom(persistentVolumeClaim: PersistentVolumeClaim seq) = this.Yield(persistentVolumeClaim)
        [<CustomOperation "add_persistentVolumeClaim">]
        member __.PersistentVolumeClaim(state: K8s, persistentVolumeClaim: PersistentVolumeClaim) = 
            state.AddResource (box (persistentVolumeClaim.ToResource()))
        
        // PersistentVolume
        member inline this.Yield(persistentVolume: PersistentVolume<CSIPersistentVolumeSource>) = this.CSIPersistentVolumeSource(K8s.empty, persistentVolume)
        member inline this.Yield(persistentVolume: PersistentVolume<CSIPersistentVolumeSource> seq) = persistentVolume |> Seq.fold (fun state x -> this.CSIPersistentVolumeSource(state, x)) K8s.empty
        member inline this.YieldFrom(persistentVolume: PersistentVolume<CSIPersistentVolumeSource> seq) = this.Yield(persistentVolume)
        [<CustomOperation "add_CsiPersistentVolume">]
        member __.CSIPersistentVolumeSource(state: K8s, persistentVolume: PersistentVolume<CSIPersistentVolumeSource>) = 
            state.AddResource (box (persistentVolume.ToResource()))
        
        
        // Namespace
        member this.Yield(ns) = this.Namespace(K8s.empty, ns)
        member this.Yield(ns) = ns |> Seq.fold (fun state x -> this.Namespace(state, x)) K8s.empty
        member this.YieldFrom(ns: Cluster.Namespace seq) = this.Yield(ns)
        [<CustomOperation "add_namespace">]
        member __.Namespace(state: K8s, ns: Cluster.Namespace) = 
            state.AddResource (box (ns.ToResource()))
        
        // ServiceAccount
        member this.Yield(serviceAccount) = this.ServiceAccount(K8s.empty, serviceAccount)
        member this.Yield(serviceAccount) = serviceAccount |> Seq.fold (fun state x -> this.ServiceAccount(state, x)) K8s.empty
        member this.YieldFrom(serviceAccount: ServiceAccount seq) = this.Yield(serviceAccount)
        [<CustomOperation "add_serviceAccount">]
        member __.ServiceAccount(state: K8s, serviceAccount: ServiceAccount) = 
            state.AddResource (box (serviceAccount.ToResource()))
        
        // ClusterRoleBinding
        member this.Yield(clusterRoleBinding) = this.ClusterRoleBinding(K8s.empty, clusterRoleBinding)
        member this.Yield(clusterRoleBinding) = clusterRoleBinding |> Seq.fold (fun state x -> this.ClusterRoleBinding(state, x)) K8s.empty
        member this.YieldFrom(clusterRoleBinding: ClusterRoleBinding seq) = this.Yield(clusterRoleBinding)
        [<CustomOperation "add_clusterRoleBinding">]
        member __.ClusterRoleBinding(state: K8s, clusterRoleBinding: ClusterRoleBinding) = 
            state.AddResource (box (clusterRoleBinding.ToResource()))
        

        [<CustomOperation "add_resource">]
        member __.Resource(state: K8s, resource: obj) = state.AddResource resource

        [<CustomOperation "add_resources">]
        member __.Resources(state: K8s, resources: obj list) = state.AddResources (resources |> List.map box)

        // K8s
        member this.Yield(k8s: K8s) = this.Append(K8s.empty, k8s)
        member this.Yield(k8s: K8s seq) = k8s |> Seq.fold (fun state x -> this.Append(state, x)) K8s.empty
        member this.YieldFrom(k8s: K8s seq) = this.Yield(k8s)
        [<CustomOperation "append">]
        member this.Append(state: K8s, toAppend: K8s) = this.Combine(state, toAppend)
        
        

    let k8s = new K8sBuilder()

    type K8sOutput = {
        isValid: bool
        content: string
        errors: ValidationProblem list
    }

    module KubeCtlWriter =
        open System
        open System.Text
        open Overboard.Serialization

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
            let output =  k8s |> toYaml
            let errs = mapErrorToMessage k8s.errors
            if not output.isValid then
                printfn "Validation errors:"

                for e in errs do
                    eprintfn "- %s" e
                printfn ""
            
            printfn "Content:"
            printfn "%s" (output.content)
    
    