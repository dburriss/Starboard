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
    image "nginx"
    command ["systemctl"]
    args ["config"; "nginx"]
}

let pod1 = pod {
    containers [container1]
    
}

let deployment1 = deployment {
    name "test-deployment"
    replicas 3
    pod pod1
    labels [("app","ngnix")]
    matchLabel ("app","nginx")
}

let k8s1 = k8s {
    deployment deployment1
}

KubeCtlWriter.print k8s1
KubeCtlWriter.toJsonFile k8s1 "nginx.deployment.json"
