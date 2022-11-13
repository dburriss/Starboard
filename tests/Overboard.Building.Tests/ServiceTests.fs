namespace Overboard.Building.Tests

open Swensen.Unquote

module ServiceTests =

    open System
    open Xunit
    open Overboard.Service


    [<Fact>]
    let ``StringOrInt can parse to int`` () =

        let sut = IntOrString.I 1
        let result = sut.Value

        Assert.Equal("1", result)

    [<Fact>]
    let ``StringOrInt is just the string`` () =

        let s = "the value"
        let sut = IntOrString.S s
        let result = sut.Value

        Assert.Equal(s, result)

    [<Fact>]
    let ``ServicePort targetPort returns number`` () =

        let sut = servicePort {
            targetPortInt 7000
        }

        Assert.Equal( 7000, Int32.Parse(sut.Spec().targetPort.Value) )
        
    [<Fact>]
    let ``ServicePort targetPort returns string`` () =

        let sut = servicePort {
            targetPortString "http-server"
        }

        Assert.Equal( "http-server", sut.Spec().targetPort.Value )


