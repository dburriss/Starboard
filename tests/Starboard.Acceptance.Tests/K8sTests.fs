namespace Starboard.Acceptance.Tests

module K8s_Resources =

    open Xunit
    open Starboard.Resources
    open Starboard.Resources.K8s
    open System.Collections.Generic
    open FSharp.Data
    open FSharp.Data.JsonExtensions

    let listsEqual<'a> expected actual = 
        let e : IEnumerable<'a> = List.toSeq expected
        let a : IEnumerable<'a> = List.toSeq actual
        Assert.Equal<'a>(e, a)

    [<Fact>]
    let ``appear in a List schema`` () =
        let container1 = container {
            image "nginx"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            containers [container1]
        }

        let deployment1 = deployment {
            podTemplate pod1
        }

        let k8s1 = k8s {
            deployment deployment1
        }

        let json = KubeCtlWriter.toJson k8s1 |> fun o -> JsonValue.Parse o.content

        Assert.Equal(json?apiVersion.AsString(), "v1")
        Assert.Equal(json?kind.AsString(), "List")
        Assert.NotEmpty(json?items.AsArray())

    [<Fact>]
    let ``K8s Deployment basics are contained in resources`` () =
        let container1 = container {
            image "nginx"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            podTemplate pod1
        }

        let k8s1 = k8s {
            deployment deployment1
        }

        let deploymentJson = KubeCtlWriter.toJson k8s1 |> fun o -> JsonValue.Parse o.content |> fun json -> json?items.AsArray() |> Array.head

        Assert.Equal(deploymentJson?apiVersion.AsString(), "apps/v1")
        Assert.Equal(deploymentJson?kind.AsString(), "Deployment")
        Assert.Equal(deploymentJson?metadata?name.AsString(), "my-name")
        Assert.NotEqual(deploymentJson?spec, JsonValue.Null)
        
    [<Fact>]
    let ``K8s to yaml`` () =
        let container1 = container {
            image "nginx"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

        let pod1 = pod {
            container container1
        }

        let deployment1 = deployment {
            name "my-name"
            podTemplate pod1
        }

        let k8s1 = k8s {
            deployment deployment1
        }

        let output = KubeCtlWriter.toYaml k8s1
        let yaml = output.content

        Assert.NotEmpty(yaml)
        Assert.Contains("apiVersion: apps/v1", yaml)
        Assert.Contains("image: nginx", yaml)
        Assert.Contains("name: my-name", yaml)
        