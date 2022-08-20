namespace Starboard.Building.Tests

module PodTests =

    open Xunit
    open Starboard.Resources
    open Starboard.Resources.K8s
    open System.Collections.Generic

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

