namespace Starboard.Building.Tests

module K8sTests =
    open Xunit
    open Swensen.Unquote

    open Starboard
    open Starboard.Workload
                                          
    [<Fact>]
    let ``append combines two k8s`` () =
        let otherk8s = k8s {
            pod {
                "my-other-pod"
            }
        }
        let sut = k8s {
            pod {
                "my-pod-1"
            }
            append otherk8s
        }

        test <@ sut.resources.Length = 2 @>

                                        
    [<Fact>]
    let ``append combines two k8s using yield`` () =

        let sut = k8s {
            pod {
                "my-pod-1"
            }
            k8s {
                pod {
                    "my-other-pod"
                }
            }
        }

        test <@ sut.resources.Length = 2 @>