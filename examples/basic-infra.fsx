#r "nuget:YamlDotNet"
#r "nuget:Newtonsoft.Json"
#r "../src/Overboard/bin/debug/net6.0/Overboard.dll"


open Overboard
open Overboard.Common
open Overboard.Workload
open Overboard.Storage
open Overboard.Service

let config = configMap {
    _name "test-config-map"
    data [
        ("allowed", "true")
        ("enemies", "aliens")
        ("lives", "3")
    ]
    add_file ("fileContent", "./some.txt")
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
    _name "pvol"
    capacity 8000<Mi>
    accessModes [ReadWriteMany]
    driver "disk.csi.azure.com"
    fsType "ext4"
}

let container1 = container {
    name "nginx"
    image "nginx:latest"
    workingDir "/test-dir"
    add_port port1
    memoryLimit 2000<Mi>
    add_volumeMount vMount
}

let appLabels = [("app","ngnix")]
let pod1 = pod {
    _labels appLabels
    cfVolume
    [container1]
}

let deployment1 = deployment {
    "test-deployment"
    _labels appLabels
    replicas 3
    podTemplate pod1
    matchLabels appLabels
}

let deployment2 = deployment {
    "test-another-deployment"
    _labels appLabels
    replicas 3
    podTemplate pod1
    matchLabels appLabels
}

let service1 = service {
    "my-service"
    add_port (servicePort {
        port 80
        targetPortInt 9376
    })
    typeOf ClusterIP
}

let ingress1 = ingress {
    ingressClassName "test-ingress"
}

let secret1 = secret {
    "my-secret-1"
}

let secret2 = secret {
    "my-secret-2"
    add_stringData ("key1", "some text")
}

let secrets = SecretList.init [secret1;secret2]

// let k8s1 = k8s {
//     configMap config
//     secretList secrets
//     service service1
//     ingress ingress1
//     deployment deployment1
//     deployment deployment2
//     persistentVolume persistentVol
// }

let k8s2 = k8s {
    add_CsiPersistentVolume persistentVol
    configMap {
        "test-config-map"
        data [
            ("allowed", "true")
            ("enemies", "aliens")
            ("lives", "3")
        ]
        add_file ("fileContent", "./some.txt")
    }
    add_secretList secrets
    secret {
        "my-secret-1"
    }
    service1;ingress1
    [ deployment1; deployment2 ]
}


KubeCtlWriter.print k8s2

let save k8s =
    let fsxName = fsi.CommandLineArgs[0].Replace(".fsx", "")
    let fileName = $"{fsxName}.deployment.json"
    KubeCtlWriter.toJsonFile k8s fileName |> ignore
    let fileName = $"{fsxName}.deployment.yaml"
    KubeCtlWriter.toYamlFile k8s fileName |> ignore

save k8s2