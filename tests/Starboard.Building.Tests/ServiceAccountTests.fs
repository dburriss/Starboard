namespace Starboard.Building.Tests

module ServiceAccountTests =

    open Xunit
    open Starboard.Common
    open Starboard.Authentication
    
    [<Fact>]
    let ``secrets are set using yield`` () = 
        let sut = serviceAccount {
            "my-service-account"
            secrets [
                objectReference {
                    fieldPath "some-field-path-1"
                }
                objectReference {
                    fieldPath "some-field-path-2"
                }
            ]
        }
        let result = sut.ToResource()

        Assert.NotEmpty(result.secrets.Value)
             
    