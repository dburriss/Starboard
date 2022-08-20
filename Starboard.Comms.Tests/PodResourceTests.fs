namespace Starboard.Comms.Tests

module DeploymentResourceTests =

    open Xunit
    open Starboard.Resources
    open Starboard.Resources.K8s
    open System.Collections.Generic
    open Swensen.Unquote

    let listsEqual<'a> expected actual = 
        let e : IEnumerable<'a> = List.toSeq expected
        let a : IEnumerable<'a> = List.toSeq actual
        Assert.Equal<'a>(e, a)

    //[<Fact>]
    //let ``Pod metadata with labels`` () =
    //    let container1 = container {
    //        name "nginx"
    //        image "nginx:1.14.2"
    //        command ["systemctl"]
    //        args ["config"; "nginx"]
    //    }

    //    let pod1 = pod {
    //        container container1
    //    }

    //    let podResource = deployment1.ToResource()
    //    let labels = deploymentResource.metadata.labels.Value.Keys |> Seq.toList
    //    let labelValues = deploymentResource.metadata.labels.Value.Values |> Seq.toList

    //    test <@ labels = ["key"] @>
    //    test <@ labelValues = ["value"] @>
    
    //[<Fact>]
    //let ``Deployment metadata with annotations`` () =
    //    let container1 = container {
    //        name "nginx"
    //        image "nginx:1.14.2"
    //        command ["systemctl"]
    //        args ["config"; "nginx"]
    //    }

    //    let pod1 = pod {
    //        container container1
    //    }

    //    let deploymentResource = deployment1.ToResource()
    //    let annotations = deploymentResource.metadata.annotations.Value.Keys |> Seq.toList
    //    let annotationValues = deploymentResource.metadata.annotations.Value.Values |> Seq.toList

    //    test <@ annotations = ["key"] @>
    //    test <@ annotationValues = ["value"] @>
