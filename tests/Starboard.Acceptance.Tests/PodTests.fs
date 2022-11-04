namespace Starboard.Acceptance.Tests
// Acceptance tests for Pod
// Question: Does the builder produce only valid K8s Ingress Resource?
// How: Use builder and verify meets Pod spec (https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/)

module K8s_Pod =

    open Xunit
    open Starboard.Common
    open Starboard.Workload
    open Starboard.Storage
    open Swensen.Unquote

    let aContainer = container {
        name "container-test"
    }

    [<Fact>]
    let ``kind is Pod`` () =
        let thePod = pod {
            _name "pod-test"
        }
        let result = thePod.ToResource()

        test <@ result.kind = "Pod" @>
    
    [<Fact>]
    let ``version is v1`` () =
        let thePod = pod {
            _name "pod-test"
        }
        let result = thePod.ToResource()

        test <@ result.apiVersion = "v1" @>
        
    [<Fact>]
    let ``metadata is set`` () =
        let thePod = pod {
            _name "pod-test"
        }
        let result = thePod.ToResource()

        test <@ result.metadata.name = "pod-test" @>
        test <@ result.metadata.``namespace`` = "default" @>
        test <@ result.metadata.annotations = None @>
        test <@ result.metadata.labels = None @>
            
    [<Fact>]
    let ``containers are set`` () =
        let thePod = pod {
            _name "pod-test"
            container {
                "container-test"
            }
        }
        let result = thePod.ToResource()

        test <@ result.spec.containers.Value.Head.name.Value = "container-test" @>
      
    [<Fact>]
    let ``persistentVolumeClaim volume is added`` () =
        let thePod = pod {
            "pod-test"
            persistentVolumeClaimVolume {
                name "emptyDir-volume"
                claimName "my-claim"
                readOnly
            }
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.name = "emptyDir-volume" @>
        test <@ result.spec.volumes.Value.Head.persistentVolumeClaim.IsSome = true @>
        test <@ result.spec.volumes.Value.Head.persistentVolumeClaim.Value.readOnly = true @>

    [<Fact>]
    let ``configMap volume is added`` () =
        let vol = configMapVolume {
                        name "configMap-projection"
                        configName "referent-name"
                    }
        let thePod = pod {
            "pod-test"
            add_volume vol
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.configMap.IsSome = true @>
        test <@ result.spec.volumes.Value.Head.name = "configMap-projection" @>
        test <@ result.spec.volumes.Value.Head.configMap.Value.name.Value = "referent-name" @>
        
    [<Fact>]
    let ``secret volume is added`` () =
        let vol = 
            secretVolume {
                name "secret-projection"
                secretName "referent-name"
            }
        let thePod = pod {
            "pod-test"
            add_volume vol
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.secret.IsSome = true @>
        test <@ result.spec.volumes.Value.Head.name = "secret-projection" @>
        test <@ result.spec.volumes.Value.Head.secret.Value.secretName.Value = "referent-name" @>

    [<Fact>]
    let ``emptyDir volume is added`` () =
        let thePod = pod {
            "pod-test"
            emptyDirVolume {
                sizeLimit 500<Mi>
            }
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.emptyDir.IsSome = true @>
        
    [<Fact>]
    let ``hostPath volume is added`` () =
        let thePod = pod {
            "pod-test"
            hostPathVolume {
                name "host-path-name"
                path "/some-path"
                hostPathType File
            }
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.hostPath.IsSome = true @>
        test <@ result.spec.volumes.Value.Head.name = "host-path-name" @>
        test <@ result.spec.volumes.Value.Head.hostPath.Value.path = "/some-path" @>
        test <@ result.spec.volumes.Value.Head.hostPath.Value.``type`` = "File" @>
    

    [<Fact>]
    let ``csi volume is added`` () =
        let thePod = pod {
            "pod-test"
            csiVolume {
                name "csi-name"
                driver "my-driver"
                fsType "Ext4"
                readOnly
                volumeAttributes [
                    "some", "attr"
                ]
            }
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.csi.IsSome = true @>
        test <@ result.spec.volumes.Value.Head.name = "csi-name" @>
        test <@ result.spec.volumes.Value.Head.csi.Value.driver = "my-driver" @>
        test <@ result.spec.volumes.Value.Head.csi.Value.fsType = "Ext4" @>
        test <@ result.spec.volumes.Value.Head.csi.Value.readOnly = true @>
        test <@ result.spec.volumes.Value.Head.csi.Value.volumeAttributes.IsSome = true @>
        test <@ result.spec.volumes.Value.Head.csi.Value.volumeAttributes.Value.Count = 1 @>
        
    [<Fact>]
    let ``with no containers is invalid`` () =
        let thePod = pod {
            "pod-test"
        }
        let result = thePod.Validate()

        Assert.NotEmpty result

    [<Fact>]
    let ``runs validation on container content`` () =
        let thePod = pod {
            "pod-test"
            container {
                "my-container"
            }
            csiVolume {
                "csi-name"
            }
        }
        let result = thePod.Validate()

        Assert.NotEmpty result
        