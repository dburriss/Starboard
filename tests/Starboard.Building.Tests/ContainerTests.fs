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

    [<Fact>]
    let ``ContainerBuilder sets workingDir`` () =
        let container1 = container {
            workingDir "test-dir"
        }

        Assert.Equal(Some "test-dir", container1.workingDir)
        
    [<Fact>]
    let ``ContainerBuilder sets port`` () =
        let port1 = containerPort {
            containerPort 5000
            hostIP "127.0.0.1"
            hostPort 80
            name "test"
            protocol TCP
        }
        let container1 = container {
            containerPort {
                containerPort 5000
                hostIP "127.0.0.1"
                hostPort 80
                name "test"
                protocol TCP
            }
        }

        Assert.Equal(Some 5000, container1.ports[0].containerPort)
        Assert.Equal(Some "127.0.0.1", container1.ports[0].hostIP)
        Assert.Equal(Some 80, container1.ports[0].hostPort)
        Assert.Equal(Some "test", container1.ports[0].name)
        Assert.Equal(TCP, container1.ports[0].protocol)

        
    [<Fact>]
    let ``ContainerBuilder sets resources`` () =
        let container1 = container {
            cpuLimit 1000<m>
            memoryLimit 500<Mi>
            cpuRequest 384<m>
            memoryRequest 250<Mi>
        }

        Assert.Equal(1000<m>, container1.resources.cpuLimit)
        Assert.Equal(500<Mi>, container1.resources.memoryLimit)
        Assert.Equal(384<m>, container1.resources.cpuRequest)
        Assert.Equal(250<Mi>, container1.resources.memoryRequest)
        
    [<Fact>]
    let ``ContainerBuilder sets volumeMount`` () =
        let container1 = container {
            name "test-container"
            volumeMount {
                name "test-volume"
                mountPath "/bin"
                readOnly
            }
        }
        let volumeMount = container1.volumeMounts[0]
        Assert.Equal(Some "test-volume", volumeMount.name)
        Assert.Equal(Some "/bin", volumeMount.mountPath)
        Assert.Equal(true, volumeMount.readOnly)

