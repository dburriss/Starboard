(**
---
title: Hello world
category: Tutorials
categoryindex: 3
index: 1
---
*)

(**
# Starboard: Hello world

In this tutorial we will create your first Starboard script and generate the Kubernetes YAML config to a file.
See 

## Initial configuration
*)

// import from Nuget
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
        "test-deployment"
        replicas 2
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
let validations = KubeCtlWriter.toYamlFile theInvalidDeployment "deployment-invalid.yaml"
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
### Fixing the config
*)

let theValidDeployment = k8s { // leave the `let` in if you are editing the existing deployment
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

(*** hide ***)
KubeCtlWriter.toYamlFile theValidDeployment "deployment-valid.yaml"
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
kubectl apply -f deployment.yaml
```

You should get the message: _deployment.apps/test-deployment created_
*)

