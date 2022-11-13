namespace Overboard.Acceptance.Tests

module K8s_Deployment =

    open Xunit
    open Overboard.Common
    open Overboard.Workload
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
            add_container container1
        }

        let deployment1 = deployment {
            _name "my-name"
            podTemplate pod1
        }

        let resource = deployment1.ToResource()
        let container = resource.spec.template.spec.Value.containers |> Option.get |> List.head

        Assert.Equal(Some "nginx", container.name)        
        Assert.Equal("nginx:1.14.2", container.image)        
        listsEqual ["systemctl"] container.command.Value   
        listsEqual ["config"; "nginx"] container.args.Value

    [<Fact>]
    let ``Deployment spec contains replicas`` () =
        let container1 = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            add_container container1
        }

        let deployment1 = deployment {
            "my-name"
            podTemplate pod1
            replicas 2
        }

        let deploymentResource = deployment1.ToResource()

        Assert.Equal(2, deploymentResource.spec.replicas)
    
    [<Fact>]
    let ``Deployment spec with no selectors`` () =

        let deployment1 = deployment {
            metadata {
                name "my-name"
            }
            pod {
                "pod-test"
                container {
                    name "nginx"
                    image "nginx:1.14.2"
                    command ["systemctl"]
                    args ["config"; "nginx"]
                }
            }
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
            container1
        }

        let deployment1 = deployment {
            "my-name"
            podTemplate pod1
            add_matchLabel ("key","value")
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
            container1
        }

        let deployment1 = deployment {
            _name "my-name"
            podTemplate pod1
            _labels [("key","value")]
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
            container1
        }

        let deployment1 = deployment {
            "my-name"
            podTemplate pod1
            _annotations [("key","value")]
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
            container1
            _labels [("pod-label","pod-label-value")]
        }

        let deployment1 = deployment {
            "my-name"
            podTemplate pod1
            _annotations [("key","value")]
        }

        let deploymentResource = deployment1.ToResource()
        let labels = deploymentResource.spec.template.metadata.Value.labels.Value.Keys |> Seq.toList
        let labelValues = deploymentResource.spec.template.metadata.Value.labels.Value.Values |> Seq.toList

        test <@ labels = ["pod-label"] @>
        test <@ labelValues = ["pod-label-value"] @>
