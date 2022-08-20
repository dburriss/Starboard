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

    [<Fact>]
    let ``Deployment spec template contains container`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
        }

        let resource = deployment1.ToResource()
        let container = resource.spec.template.spec.containers |> Option.get |> List.head

        Assert.Equal(Some "nginx", container.name)        
        Assert.Equal("nginx:1.14.2", container.image)        
        listsEqual ["systemctl"] container.command      
        listsEqual ["config"; "nginx"] container.args

    [<Fact>]
    let ``Deployment spec contains replicas`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
            replicas 2
        }

        let deploymentResource = deployment1.ToResource()

        Assert.Equal(2, deploymentResource.spec.replicas)
    
    [<Fact>]
    let ``Deployment spec with no selectors`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
        }

        let deploymentResource = deployment1.ToResource()

        Assert.Equal(None, deploymentResource.spec.selector)
    
    [<Fact>]
    let ``Deployment spec with matchLabels`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
            matchLabel ("key","value")
        }

        let deploymentResource = deployment1.ToResource()
        let matchLabelsKeys = deploymentResource.spec.selector.Value.matchLabels.Value.Keys |> Seq.toList
        let matchLabelsValues = deploymentResource.spec.selector.Value.matchLabels.Value.Values |> Seq.toList

        Assert.Equal(None, deploymentResource.spec.selector.Value.matchExpressions)
        test <@ matchLabelsKeys = ["key"] @>
        test <@ matchLabelsValues = ["value"] @>
    
    [<Fact>]
    let ``Deployment metadata with labels`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
            labels [("key","value")]
        }

        let deploymentResource = deployment1.ToResource()
        let labels = deploymentResource.metadata.labels.Value.Keys |> Seq.toList
        let labelValues = deploymentResource.metadata.labels.Value.Values |> Seq.toList

        test <@ labels = ["key"] @>
        test <@ labelValues = ["value"] @>
    
    [<Fact>]
    let ``Deployment metadata with annotations`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
            annotations [("key","value")]
        }

        let deploymentResource = deployment1.ToResource()
        let annotations = deploymentResource.metadata.annotations.Value.Keys |> Seq.toList
        let annotationValues = deploymentResource.metadata.annotations.Value.Values |> Seq.toList

        test <@ annotations = ["key"] @>
        test <@ annotationValues = ["value"] @>
    
    [<Fact>]
    let ``Deployment pod template labels`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
            labels [("pod-label","pod-label-value")]
        }

        let deployment1 = deployment {
            name "my-name"
            pod pod1
            annotations [("key","value")]
        }

        let deploymentResource = deployment1.ToResource()
        let labels = deploymentResource.spec.template.metadata.Value.labels.Value.Keys |> Seq.toList
        let labelValues = deploymentResource.spec.template.metadata.Value.labels.Value.Values |> Seq.toList

        test <@ labels = ["pod-label"] @>
        test <@ labelValues = ["pod-label-value"] @>
