(**
---
title: Run a script in a Pod
description: Run a script file
category: Tutorials
categoryindex: 3
index: 4
---
*)

(**
# Running a script in a Pod

In this tutorial we will look at running a script in a `Pod`, running it as a `Job` and then finally running that `Job` as a `CronJob`.

## Topics covered

- [Deployments](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
- [Jobs](https://kubernetes.io/docs/concepts/workloads/controllers/job/)
- [CronJobs](https://kubernetes.io/docs/concepts/workloads/controllers/cron-jobs/)
- [ConfigMaps](https://kubernetes.io/docs/concepts/configuration/configmap/)

## Requirements

1. A [Kubernetes cluster to learn on](https://kubernetes.io/docs/tasks/tools/) such as Kubernetes on Docker or minikube.
1. A basic knowledge of using [kubectl](https://kubernetes.io/docs/reference/kubectl/)
1. [Ingress enabled](https://kubernetes.github.io/ingress-nginx/deploy/) for your clusterW
1. [dotnet SDK](https://dotnet.microsoft.com/en-us/) installed
1. Optionally, any IDE that supports F# (Visual Studio Code, IntelliJ Rider, Visual Studio, NeoVim)

> Visual Studio Code with the [Ionide](https://ionide.io/) is a great choice. See [Setup your environment](../setup-environment.fsx) for more details.

## Shipping a script in a Deployment

In this first step we are going to show what is required to bundle a script into a `Deployment` and run it in a `Pod`.

Overboard had a nice addition on `ConfigMap`s that allows you to add a file from the machine creating the config.

The file we will be adding to the `ConfigMap` is called _.write-date.fsx_ and has the following contents:

> Note: The fsx file has a . at the start of it's name purely so it is ignored by this documentation system. It is not required you place a . at the start of your filename.

```fsharp
open System
Console.WriteLine($"Hello from FSX script at {DateTimeOffset.UtcNow}")
```

To leverage this script we create the following resources:
*)
//#r "nuget:Overboard"
#r "nuget:YamlDotNet"
#r """..\..\src\Overboard\bin\Debug\net6.0\Overboard.dll"""

open System
open Overboard
open Overboard.Common
open Overboard.Workload
open Overboard.Storage

let scriptPath = IO.Path.Combine(".write-date.fsx")
let labels = ["app","script"]
let k8sDeploymentConfig = 
    k8s {
        // config map containing write-date.fsx
        configMap {
            _name "script-configmap"
            add_file ("write-date.fsx", scriptPath)
        }
        // a deployment
        deployment {
            _name "script-deployment"
            add_matchLabels labels
            pod {
                _name "script-pod"
                _labels labels
                // container with an image with dotnet that runs the script 
                container {
                    name "script-runner"
                    image "mcr.microsoft.com/dotnet/sdk:7.0-alpine"
                    command ["dotnet"]
                    args ["fsi"; "./.write-date.fsx"]
                    workingDir "/scripts"
                    volumeMount {
                        name "script-volume"
                        mountPath "/scripts"
                    }
                }
                // configMap is mounted as a volume
                configMapVolume {
                    name "script-volume"
                    configName "script-configmap"
                    defaultMode 0700
                }
            }
        }
    }

// write the config YAML file
let configDir = __SOURCE_DIRECTORY__
KubeCtlWriter.toYamlFile k8sDeploymentConfig (IO.Path.Combine( configDir, "script-deployment.yaml"))

(**
See how in the `ConfigMap` definition we use the `add_file` operation.

```fsharp
add_file ("write-date.fsx", scriptPath)
```

The first element of the tuple is the name of the file as it will appear in the `ConfigMap` (which will be mounted as a file in the volume). 
The second element is the path to the file on the machine that will be building this config. 
In this case, your machine. In a production environment this may be a CI build agent.

Let's test this `Deployment` out:

```bash
kubectl apply -f .\script-deployment.yaml
```

Here is an example of what the run can look like

```bash
> kubectl get pods
NAME                                 READY   STATUS      RESTARTS      AGE
script-deployment-655f668d57-g777w   0/1     Completed   5 (85s ago)   2m53s

> kubectl logs script-deployment-655f668d57-g777w
Hello from FSX script at 11/20/2022 11:17:40 +00:00
```

We can see from the logs that the script has been run (use your pod name when fetching the logs).

## Using a Job

We can create a `Deployment` and run a script like we did but Kubernetes has a resource that is a better match for this kind of task. 
A Kubernetes [Job](https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/job-v1/) is specifically for a single execution workload like we a re doing here.
One reason to prefer a Job is the cleanup of the pods after the work is done.

You could swap out the deployment for a `Job` leaving the `Pod` and `ConfigMap` unchanged but Overboard has a simpler way to use a `Job`. 
In the `Overboard.Extras` namespace is a high-level resource called `fsJob` which takes a F# script file as am `entryPoint` and will create a `Job` from it.
*)

open Overboard.Extras

let fsJobConfig = fsJob {
        entryPoint "./.write-date.fsx"
    }

KubeCtlWriter.toYamlFile fsJobConfig (IO.Path.Combine( configDir, "script-job.yaml"))

(**
This can be a very simple way to run one-off jobs.

```bash
> kubectl apply -f .\script-job.yaml
> kubectl get pods
script-write-date-job-4gstg          0/1     Completed          0               2m12s
> kubectl logs script-write-date-job-4gstg
Hello from FSX script at 11/20/2022 19:52:29 +00:00
```

## Creating a CronJob

If we had manually created a `Job` we could use that job to create a `CronJob`. 
With the `fsJob` abstraction through all you need do is add a Cron schedule and your `Job` will become a `CronJob`.
*)

let fsCronJobConfig = fsJob {
        entryPoint "./.write-date.fsx"
        schedule "* * * * *"
    }

KubeCtlWriter.toYamlFile fsCronJobConfig (IO.Path.Combine( configDir, "script-cronjob.yaml"))

(**
Run the `get all` command to see what is created. Each occurrence of the `Job` runs in a new `Pod`.

```bash
kubectl apply -f .\script-cronjob.yaml
kubectl get all
```

## Conclusion

In this tutorial we created a `Deployment` that runs a script once. 
You then leveraged `fsJob` abstraction to easily run the same script as a `Job`.
You then easily changed that `Job` to a `CronJob` by adding a `schedule`.
*)