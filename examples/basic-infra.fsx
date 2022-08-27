#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../src/Starboard/bin/debug/net6.0/Starboard.dll"

// apiVersion: apps/v1
// kind: Deployment
// metadata:
//   name: nginx-deployment
//   labels:
//     app: nginx
// spec:
//   replicas: 3
//   selector:
//     matchLabels:
//       app: nginx
//   template:
//     metadata:
//       labels:
//         app: nginx
//     spec:
//       containers:
//       - name: nginx
//         image: nginx:1.14.2
//         ports:
//         - containerPort: 80

open Starboard.Resources
open Starboard.Resources.K8s

let container1 = container {
    name "nginx"
    image "nginx:latest"
    workingDir "/test-dir"
}

let appLabels = [("app","ngnix")]
let pod1 = pod {
    containers [container1]
    labels appLabels
}

let deployment1 = deployment {
    name "test-deployment"
    replicas 3
    pod pod1
    labels appLabels
    matchLabels appLabels
}

let deployment2 = deployment {
    name "test-another-deployment"
    replicas 3
    pod pod1
    labels appLabels
    matchLabels appLabels
}

let k8s1 = k8s {
    deployment deployment1
    deployment deployment2
}

KubeCtlWriter.print k8s1

let save k8s =
    let fsxName = fsi.CommandLineArgs[0].Replace(".fsx", "")
    let fileName = $"{fsxName}.deployment.yaml"
    KubeCtlWriter.toYamlFile k8s fileName

save k8s1 