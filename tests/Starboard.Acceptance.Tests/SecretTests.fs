namespace Starboard.Acceptance.Tests

open System.Text

// Acceptance tests for Secret
// Question: Does the builder produce only valid K8s Secret Resource?
// How: Use builder and verify meets Secret spec (https://kubernetes.io/docs/reference/kubernetes-api/config-and-storage-resources/secret-v1/)

module K8s_Secret =

    open Xunit
    open Starboard
    open Starboard.Resources
    open Swensen.Unquote

    [<Fact>]
    let ``kind is Secret`` () =
        let sut = secret {
            "my-secret"
        }
        let result = sut.ToResource()

        test <@ result.kind = "Secret" @>
       
    [<Fact>]
    let ``apiVersion is v1`` () =
        let sut = secret {
            _name "my-secret"
        }
        let result = sut.ToResource()

        test <@ result.apiVersion = "v1" @>
              
    [<Fact>]
    let ``name is set in metadata`` () =
        let sut = secret {
            "my-secret"
        }
        let result = sut.ToResource()

        test <@ result.metadata.name = "my-secret" @>
                  
    [<Fact>]
    let ``immutable is false by default`` () =
        let sut = secret {
            "my-secret"
        }
        let result = sut.ToResource()
        test <@ result.immutable = false  @>
                  
    [<Fact>]
    let ``immutable is true when set`` () =
        let sut = secret {
            "my-secret"
            immutable
        }
        let result = sut.ToResource()
        test <@ result.immutable = true  @>
                  
    [<Fact>]
    let ``type is Opaque`` () =
        let sut = secret {
            "my-secret"
        }
        let result = sut.ToResource()
        test <@ result.``type`` = "Opaque"  @>
                  
    [<Fact>]
    let ``default map data is None`` () =
        let sut = secret {
            "my-secret"
        }
        let result = sut.ToResource()

        test <@ result.data = None  @>
        test <@ result.stringData = None  @>
                          
    [<Fact>]
    let ``data is saved as base64`` () =
        let sut = secret {
            "my-secret"
            data [
                ("k1", Encoding.UTF8.GetBytes("some text"))
            ]
        }
        let result = sut.ToResource()
        let value = result.data.Value.Item("k1")
        test <@ (String.isBase64 value) = true @>

                  
    [<Fact>]
    let ``stringData is set`` () =
        let sd = [
            "k1", "v1"
            "k2", "v2"
        ]
        let sut = secret {
            "my-secret"
            stringData sd
        }
        let expected = sd |> Map.ofList |> Helpers.mapToIDictionary |> Some
        let result = sut.ToResource()
        test <@ result.stringData = expected  @>

       