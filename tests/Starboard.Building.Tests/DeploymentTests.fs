namespace Starboard.Building.Tests

module DeploymentTests =

    open Xunit
    open Starboard.Resources
    open Starboard.Resources.K8s
    open System.Collections.Generic

    let listsEqual<'a> expected actual = 
        let e : IEnumerable<'a> = List.toSeq expected
        let a : IEnumerable<'a> = List.toSeq actual
        Assert.Equal<'a>(e, a)

    let aContainer = container {
        image "nginx"
        command ["systemctl"]
        args ["config"; "nginx"]
    }
    let aPod = pod {
        containers [aContainer]
    }

    [<Fact>]
    let ``DeploymentBuilder creates a basic deployment`` () =

        let pod1 = pod {
            containers [aContainer]
        }
        let deployment1 = deployment {
            podTemplate aPod
        }

        Assert.Equal(Some pod1, deployment1.pod)

    [<Fact>]
    let ``DeploymentBuilder with a name sets name`` () =

        let deployment1 = deployment {
            name "my-name"
            podTemplate aPod
        }

        Assert.Equal("my-name", deployment1.metadata.name)

    [<Fact>]
    let ``DeploymentBuilder sets replicas`` () =

        let deployment1 = deployment {
            replicas 2
            podTemplate aPod
        }

        Assert.Equal(2, deployment1.replicas)


    [<Fact>]
    let ``DeploymentBuilder sets namespace`` () =

        let deployment1 = deployment {
            ns "test"
            podTemplate aPod
        }

        Assert.Equal("test", deployment1.metadata.ns)

    [<Fact>]
    let ``DeploymentBuilder sets labels`` () =
        let expected = [("key","value")]
        let deployment1 = deployment {
            labels expected
            podTemplate aPod
        }

        listsEqual expected deployment1.metadata.labels
        
    [<Fact>]
    let ``DeploymentBuilder sets annotations`` () =
        let expected = [("key","value")]
        let deployment1 = deployment {
            annotations expected
            podTemplate aPod
        }

        listsEqual expected deployment1.metadata.annotations
    
    [<Fact>]
    let ``DeploymentBuilder sets matchExpressions with selectors`` () =
        let expected  = [{ key = "key"; operator = In; values = ["value"] }]
        let labelsToMatch = selector {
            matchIn ("key",["value"])   
        }
        let deployment1 = deployment {
            selector labelsToMatch
            podTemplate aPod
        }

        listsEqual expected deployment1.selector.matchExpressions
        
    [<Fact>]
    let ``DeploymentBuilder sets matchLabels with selectors`` () =
        let expected = [("key","value")]
        let labelsToMatch = selector {
            matchLabel ("key","value")   
        }
        let deployment1 = deployment {
            selector labelsToMatch
            podTemplate aPod
        }

        listsEqual expected deployment1.selector.matchLabels


    [<Fact>]
    let ``DeploymentBuilder sets matchLabels with matchLabel`` () =
        let expected = [("key","value")]

        let deployment1 = deployment {
            matchLabel ("key","value")
            podTemplate aPod
        }

        listsEqual expected deployment1.selector.matchLabels



