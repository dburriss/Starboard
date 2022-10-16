#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../src/Starboard/bin/debug/net6.0/Starboard.dll"


open Starboard.Resources
open Starboard.Resources.K8s

let config = configMap {
    name "test-config-map"
    data [
        ("allowed", "true")
        ("enemies", "aliens")
        ("lives", "3")
    ]
}

let port1 = containerPort {
    hostIP "127.0.0.1"
}

let vMount = volumeMount {
    name "config-vol"
    mountPath "/etc/config"
    readOnly
}

let cfVolume = configMapVolume {
    name "config-vol"
    item ("log_level", "log_level.conf")
}

let persistentVol = csi {
    name "pvol"
    capacity 8000<Mi>
    accessModes [ReadWriteMany]
    driver "disk.csi.azure.com"
    fsType "ext4"
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
    volume cfVolume
}

let deployment1 = deployment {
    name "test-deployment"
    replicas 3
    podTemplate pod1
    labels appLabels
    matchLabels appLabels
}

let deployment2 = deployment {
    name "test-another-deployment"
    replicas 3
    podTemplate pod1
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

let ingress1 = ingress {
    ingressClassName "test-ingress"
}

let k8s1 = k8s {
    configMap config
    service service1
    ingress ingress1
    deployment deployment1
    deployment deployment2
    persistentVolume persistentVol
}

KubeCtlWriter.print k8s1

let save k8s =
    let fsxName = fsi.CommandLineArgs[0].Replace(".fsx", "")
    let fileName = $"{fsxName}.deployment.json"
    KubeCtlWriter.toJsonFile k8s fileName |> ignore
    let fileName = $"{fsxName}.deployment.yaml"
    KubeCtlWriter.toYamlFile k8s fileName |> ignore

save k8s1 