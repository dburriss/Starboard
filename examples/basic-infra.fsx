#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../src/Starboard/bin/debug/net6.0/Starboard.dll"


open Starboard.Resources
open Starboard.Resources.K8s
open Starboard.Resources.Services

let port1 = containerPort {
    hostIP "127.0.0.1"
}

let vMount = volumeMount {
    name "test-vmount"
    mountPath "/bin"
    readOnly
}

let container1 = container {
    name "nginx"
    image "nginx:latest"
    workingDir "/test-dir"
    port port1
    memoryLimit 2000<Mi>
    volumeMount vMount
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

let service1 = service {
    name "my-service"
    port (servicePort {
        port 80
        targetPortInt 9376
    })
    typeOf ClusterIP
}

let k8s1 = k8s {
    service service1
    deployment deployment1
    deployment deployment2
}

KubeCtlWriter.print k8s1

let save k8s =
    let fsxName = fsi.CommandLineArgs[0].Replace(".fsx", "")
    let fileName = $"{fsxName}.deployment.json"
    KubeCtlWriter.toJsonFile k8s fileName
    let fileName = $"{fsxName}.deployment.yaml"
    KubeCtlWriter.toYamlFile k8s fileName

save k8s1 