namespace Starboard.Acceptance.Tests

open System.Text

// Acceptance tests for Secret
// Question: Does the builder produce only valid K8s Secret Resource?
// How: Use builder and verify meets Secret spec (https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/secret-v1/)

module K8s_ConfigMap =

    open Xunit
    open Starboard
    open Starboard.Resources
    open Swensen.Unquote

    [<Fact>]
    let ``kind is ConfigMap`` () =
        let sut = configMap {
            "my-config"
        }
        let result = sut.ToResource()

        test <@ result.kind = "ConfigMap" @>
       
    [<Fact>]
    let ``apiVersion is v1`` () =
        let sut = configMap {
            _name "my-config"
        }
        let result = sut.ToResource()

        test <@ result.apiVersion = "v1" @>
              
    [<Fact>]
    let ``name is set in metadata`` () =
        let sut = configMap {
            "my-config"
        }
        let result = sut.ToResource()

        test <@ result.metadata.name = "my-config" @>
                  
    [<Fact>]
    let ``immutable is false by default`` () =
        let sut = configMap {
            "my-config"
        }
        let result = sut.ToResource()
        test <@ result.immutable = false  @>
        
    [<Fact>]
    let ``default map data is None`` () =
        let sut = configMap {
            "my-config"
        }
        let result = sut.ToResource()

        test <@ result.data = None  @>
        test <@ result.binaryData = None  @>
                      
    [<Fact>]
    let ``stringData is set`` () =
        let sd = [
            "k1", "v1"
            "k2", "v2"
        ]
        let sut = secret {
            "my-config"
            stringData sd
        }
        let expected = sd |> Map.ofList |> Helpers.mapToIDictionary |> Some
        let result = sut.ToResource()
        test <@ result.stringData = expected  @>

       