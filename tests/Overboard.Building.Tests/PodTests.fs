namespace Overboard.Building.Tests

module PodTests =

    open Xunit
    open Overboard.Common
    open Overboard.Workload
    open Overboard.Storage
    open System.Collections.Generic
    open Swensen.Unquote

    let listsEqual<'a> expected actual = 
        let e : IEnumerable<'a> = List.toSeq expected
        let a : IEnumerable<'a> = List.toSeq actual
        Assert.Equal<'a>(e, a)


    [<Fact>]
    let ``PodBuilder creates a basic basic pod`` () =
        let container1 = container {
            image "nginx"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            containers [container1]
        }

        listsEqual [container1] pod1.containers
    
    [<Fact>]
    let ``csi volume parameters are added when using implicit yield`` () =
        let thePod = pod {
            "pod-test"
            csiVolume {
                [
                    "some", "attr"
                ]
            }
        }
        let result = thePod.ToResource()

        Assert.True(result.spec.volumes.Value.Head.csi.Value.volumeAttributes.IsSome)
        Assert.Equal(1, result.spec.volumes.Value.Head.csi.Value.volumeAttributes.Value.Count)
      

      
    [<Fact>]
    let ``configVolume defaultMode is octal`` () =
        let thePod = pod {
            "pod-test"
            configMapVolume {
                defaultMode 0700
            }
        }
        let result = thePod.ToResource()

        test <@ result.spec.volumes.Value.Head.configMap.IsSome @>
        test <@ result.spec.volumes.Value.Head.configMap.Value.defaultMode = Some "0700" @>
      

