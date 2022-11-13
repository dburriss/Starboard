(**
---
title: Generating Kubernetes config
category: How-to
categoryindex: 2
index: 2
---
*)

(**
# Generating Kubernetes config

Overboard gives a powerful way to define your Kubernetes config but for that to be useful, we need to generate the kubernetes config. Overboard provides some helpers for doing just this in `cref:T:Overboard.K8s.KubeCtlWriter`.
*)
(*** hide ***)
// import from Nuget
#r "nuget:Overboard"

// open the required namespaces

open Overboard
open Overboard.Common
open Overboard.Workload

(*** show ***)
let kubeConfig = k8s {
    deployment {
        "my-name" // <- No operation specified
        _labels ["label1", "value1"]
    }
}

(**
## Printing to screen

If you would like to see what is generated on your screen you can use the `cref:T:Overboard.K8s.KubeCtlWriter.print` function. 

> Note that the print also prints any validation errors.
*)

KubeCtlWriter.print kubeConfig
(*** hide ***)
let k8sOutput' = KubeCtlWriter.toYaml kubeConfig
k8sOutput'.content
(*** include-it ***)
(**
## Writing to file

If you would like to save your `cref:T:Overboard.K8s` config to file, you can use either the `cref:T:Overboard.K8s.KubeCtlWriter``.toJsonFile` or `cref:T:Overboard.K8s.KubeCtlWriter``.toYamlFile` function.
As the names imply, these will save your config as JSON or YAML respectively.
*)

(**
```fsharp
KubeCtlWriter.toYamlFile kubeConfig "deployment.yaml"
```
*)

(**
## Getting the content

You can also get the `cref:T:Overboard.K8s.K8sOutput`, which gives you access to not only the JSON or YAML `cref:T:Overboard.K8s.K8sOutput``.content`, but also to the `cref:T:Overboard.K8s.K8sOutput``.errors`. 
This is an array of validation errors that Overboard has found in the schema. Overboard prevents many schema issues by being strongly typed but it also runs validation on values that should not be empty or should follow certain formatting.
It will still generate the config you specify but gives you errors that you can check before submitting the config to a Kubernetes cluster.
*)


let k8sOutput = KubeCtlWriter.toYaml kubeConfig
k8sOutput.content
(*** include-it ***)

(**
## Validation errors

As mentioned, `cref:T:Overboard.K8s.K8sOutput` contains an `errors` field that contains a list of validation errors.

Each `cref:T:Overboard.ValidationProblem` has a `Message` field on it that gives back a `string`. You can print these errors out:
*)

k8sOutput.errors 
|> List.map (fun err -> err.Message)
|> List.iter (eprintfn "%s")