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

Starboard gives a powerful way to define your Kubernetes config but for that to be useful, we need to generate the kubernetes config. Starboard provides some helpers for doing just this in `cref:T:Starboard.K8s.KubeCtlWriter`.
*)
(*** hide ***)
// import from Nuget
#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../src/Starboard/bin/debug/net6.0/Starboard.dll"

// open the required namespaces

open Starboard
open Starboard.Common
open Starboard.Workload

(*** show ***)
let kubeConfig = k8s {
    deployment {
        "my-name" // <- No operation specified
        _labels ["label1", "value1"]
    }
}

(**
## Printing to screen

If you would like to see what is generated on your screen you can use the `cref:T:Starboard.K8s.KubeCtlWriter.print` function.
*)

KubeCtlWriter.print kubeConfig
(*** hide ***)
let k8sOutput' = KubeCtlWriter.toYaml kubeConfig
k8sOutput'.content
(*** include-it ***)

(**
## Writing to file

If you would like to save your `cref:T:Starboard.K8s` config to file, you can use either the `cref:T:Starboard.K8s.KubeCtlWriter``.toJsonFile` or `cref:T:Starboard.K8s.KubeCtlWriter``.toYamlFile` function.
As the names imply, these will save your config as JSON or YAML respectively.
*)

(**
```fsharp
KubeCtlWriter.toYamlFile kubeConfig "deployment.yaml"
```
*)

(**
## Getting the content

You can also get the `cref:T:Starboard.K8s.K8sOutput`, which gives you access to not only the JSON or YAML `cref:T:Starboard.K8s.K8sOutput``.content`, but also to the `cref:T:Starboard.K8s.K8sOutput``.errors`. 
This is an array of validation errors that Starboard has found in the schema. Starboard prevents many schema issues by being strongly typed but it also runs validation on values that should not be empty or should follow certain formatting.
It will still generate the config you specify but gives you errors that you can check before submitting the config to a Kubernetes cluster.
*)


let k8sOutput = KubeCtlWriter.toYaml kubeConfig
(*** hide ***)
k8sOutput
(*** include-it ***)