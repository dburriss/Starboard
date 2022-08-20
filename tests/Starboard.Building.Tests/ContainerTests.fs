namespace Starboard.Building.Tests

module ContainerTests =

    open Xunit
    open Starboard.Resources
    open System.Collections.Generic

    let listsEqual<'a> expected actual = 
        let e : IEnumerable<'a> = List.toSeq expected
        let a : IEnumerable<'a> = List.toSeq actual
        Assert.Equal<'a>(e, a)

    [<Fact>]
    let ``ContainerBuilder creates a basic container with image, args, and command`` () =
        let container1 = container {
            image "nginx"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        Assert.Equal("nginx", container1.image)
        listsEqual ["systemctl"] container1.command
        listsEqual ["config"; "nginx"] container1.args

