namespace Starboard.Acceptance.Tests
// Acceptance tests for Pod
// Question: Does the builder produce only valid K8s Ingress Resource?
// How: Use builder and verify meets Pod spec (https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/)

module K8s_Pod =

    open Xunit
    open Starboard
    open Starboard.Resources
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
    let ``volumes are set`` () =
        let thePod = pod {
            "pod-test"
            emptyDirVolume {
                sizeLimit 500<Mi>
            }
        }
        let result = thePod.ToResource()

        Assert.True result.spec.volumes.Value.Head.emptyDir.IsSome
