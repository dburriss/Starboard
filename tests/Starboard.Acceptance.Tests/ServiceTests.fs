namespace Starboard.Acceptance.Tests
// Acceptance tests for Ingress
// Question: Does the builder produce only valid K8s Ingress Resource?
// How: Use builder and verify meets Ingress spec (https://kubernetes.io/docs/reference/kubernetes-api/service-resources/ingress-v1/)

module K8s_Service =

    open Xunit
    open Starboard
    open Starboard.Resources
    open Swensen.Unquote

    [<Fact>]
    let ``kind is Service`` () =
        let theService = service {
            "service-test"
        }
        let result = theService.ToResource()

        test <@ result.kind = "Service" @>
        
    [<Fact>]
    let ``version is v1`` () =
        let theService = service {
            "service-test"
        }
        let result = theService.ToResource()

        test <@ result.apiVersion = "v1" @>
