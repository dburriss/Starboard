---
title: Home
---
# Home Page

This file will load as the root of your docs.

## Getting started

Create a F# script file, for example `infra.fsx`.

```fsharp
// infra.fsx
#r "nuget:Starboard"

open Starboard.Common
open Starboard.Workloads

let theDeployment = k8s {
    deployment {
        "test-deployment"
        replicas 3
        pod {
            container {
                name "nginx"
                image "nginx:latest"
                workingDir "/test-dir"
            }
        }
    }
}

KubeCtlWriter.toYamlFile theDeployment "infra.yaml" |> ignore
```

To explore more examples, check out the [tutorials](tutorials/index.md).

## About this documentation

The docs follow the guidance from [The documentation system](https://documentation.divio.com/).