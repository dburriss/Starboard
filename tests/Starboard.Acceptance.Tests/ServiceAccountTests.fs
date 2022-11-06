namespace Starboard.Acceptance.Tests


module K8s_ServiceAccount =

    open Xunit
    open Starboard.Common
    open Starboard.Authentication
    open Swensen.Unquote

    [<Fact>]
    let ``kind is ServiceAccount`` () =
        let sut = serviceAccount {
            "my-service-account"
        }
        let result = sut.ToResource()

        test <@ result.kind = "ServiceAccount" @>
       
    [<Fact>]
    let ``apiVersion is v1`` () =
        let sut = serviceAccount {
            "my-service-account"
        }
        let result = sut.ToResource()

        test <@ result.apiVersion = "v1" @>
              
    [<Fact>]
    let ``name is set in metadata`` () =
        let sut = serviceAccount {
            "my-service-account"
        }
        let result = sut.ToResource()

        test <@ result.metadata.name = "my-service-account" @>
                  
    [<Fact>]
    let ``automountServiceAccountToken is false by default`` () =
        let sut = serviceAccount {
            "my-service-account"
        }
        let result = sut.ToResource()
        test <@ result.automountServiceAccountToken = false  @>
                  
    [<Fact>]
    let ``automountServiceAccountToken is true when set`` () =
        let sut = serviceAccount {
            "my-service-account"
            automountServiceAccountToken
        }
        let result = sut.ToResource()
        test <@ result.automountServiceAccountToken = true  @>
                         
    [<Fact>]
    let ``default imagePullSecrets None`` () =
        let sut = serviceAccount {
            "my-service-account"
        }
        let result = sut.ToResource()

        test <@ result.imagePullSecrets = None  @>
                                   
    [<Fact>]
    let ``imagePullSecrets are set`` () =
        let sut = serviceAccount {
            "my-service-account"
            imagePullSecrets [ "i-secret-1"; "i-secret-2" ]
        }
        let result = sut.ToResource()

        test <@ result.imagePullSecrets = Some [ { name = Some "i-secret-1" }; { name = Some "i-secret-2" } ] @>
                           
    [<Fact>]
    let ``secrets imagePullSecrets None`` () =
        let sut = serviceAccount {
            "my-service-account"
        }
        let result = sut.ToResource()

        test <@ result.secrets = None  @>
    
                                       
    [<Fact>]
    let ``secrets are set`` () = 
        let secrets' = [
                objectReference {
                    fieldPath "some-field-path-1"
                }
                objectReference {
                    fieldPath "some-field-path-2"
                }
            ]
        let sut = serviceAccount {
            "my-service-account"
            secrets secrets'
        }
        let result = sut.ToResource()

        test <@ result.secrets.Value.Length = 2 @>
        test <@ result.secrets.Value.Head.fieldPath = Some "some-field-path-1" @>
