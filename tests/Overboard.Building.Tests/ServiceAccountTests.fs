namespace Overboard.Building.Tests

module ServiceAccountTests =

    open Xunit
    open Overboard.Common
    open Overboard.Authentication
    
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
             
    