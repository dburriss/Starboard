(**
---
title: Hello world
description: Generate your first Deployment
category: Tutorials
categoryindex: 3
index: 1
---
*)

(**
# Starboard: Hello world

In this tutorial we will create your first Starboard script and generate the Kubernetes YAML config to a file.

## Requirements

1. A [Kubernetes cluster to learn on](https://kubernetes.io/docs/tasks/tools/) such as Kubernetes on Docker or minikube.
2. A basic knowledge of using [kubectl](https://kubernetes.io/docs/reference/kubectl/)
3. [dotnet SDK]() installed
4. Optionally, any IDE that supports F# (Visual Studio Code, IntelliJ Rider, Visual Studio, NeoVim)

> Visual Studio Code with the [Ionide](https://ionide.io/) is a great choice. See [Setup your environment](../setup-environment.fsx) for more details.

## Initial configuration

Create a F# script file called `deployment.fsx`

Copy the following code into the script file:
*)

// TODO: import from Nuget
#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../../src/Starboard/bin/debug/net6.0/Starboard.dll"

// open the required namespaces
open Starboard
open Starboard.Common
open Starboard.Workload

// define your k8s config
let theInvalidDeployment = k8s {
    deployment {
        pod {
            container {
                name "nginx"
                image "nginx:latest"
                workingDir "/test-dir"
            }
        }
    }
}

// Write the YAML to infra.yaml file and get the list of validation issues
let validations = KubeCtlWriter.toYamlFile theInvalidDeployment "hello-world.yaml"
// Let's print out the validation errors
for err in validations do
    eprintfn "%s" err.Message

(*** hide ***)
let output1 = KubeCtlWriter.toYaml theInvalidDeployment
let errorOutput1 = 
    output1.errors 
    |> List.map (fun err -> err.Message)
    |> List.toSeq 
    |> fun arr -> String.concat (System.Environment.NewLine) arr
(**
### Output
*)
(*** include-value: errorOutput1 ***)

(**

Now you can call the fsx file to generate your YAML config.

```bash
dotnet fsi deployment.fsx
```

If you run the apply command on your YAML file, you will see Kubernetes agrees with the validation errors.

```bash
kubectl apply -f hello-world.yaml
```

## Fixed config

Let's address the validation errors that Starboard found. 

1. Call the `add_matchLabel` operation with a key/value pair for the label. 
2. Next, add the label to the pod using the `_labels` metadata operation, passing in a list of key/value pairs.

*)

let theValidDeployment = k8s { 
    deployment {
        "test-deployment"
        replicas 2
        add_matchLabel ("app", "nginx") // <- fix the validation error
        pod {
            _labels [("app", "nginx")] // <- fix the validation error
            container {
                name "nginx"
                image "nginx:latest"
                workingDir "/test-dir"
            }
        }
    }
}

KubeCtlWriter.toYamlFile theValidDeployment "hello-world.yaml"
(*** hide ***)
let output2 = KubeCtlWriter.toYaml theValidDeployment
let content2 = output2.content
(**
### Output

Our validation errors are gone and we have a valid Kubernetes configuration.
*)
(*** include-value: content2 ***)

(**
## Testing the result

Let's execute this deployment against our Kubernetes cluster to confirm it is indeed correct.

```bash
dotnet fsi deployment.fsx
kubectl apply -f hello-world.yaml
kubectl get deployments
```

You should get the message: _deployment.apps/test-deployment created_

## Summary

In this tutorial you created your first Starboard script and generated the YAML. We saw how to get and print out the validation errors.
Finally, we saw how we can successfully deploy our generated script.

Congratulations! You have taken a turn toward a new way of configuring your infrastructure.
*)

