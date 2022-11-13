namespace Starboard.Acceptance.Tests
// Acceptance tests for Ingress
// Question: Does the builder produce only valid K8s Ingress Resource?
// How: Use builder and verify meets Ingress spec (https://kubernetes.io/docs/reference/kubernetes-api/service-resources/ingress-v1/)

module K8s_Service =

    open Xunit
    open Starboard.Common
    open Starboard.Service

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
        
    [<Fact>]
    let ``ports targetPort has string value`` () =
        let servicePort1 = servicePort {
            targetPortString "http-server"
        }
        let theService = service {
            "service-test"
            add_port servicePort1
        }
        let result = theService.ToResource()

        test <@ result.spec.ports.Value[0].targetPort.Value = "http-server" @>
                
    [<Fact>]
    let ``selector has key value map of labels`` () =
        let theService = service {
            "service-test"
            matchLabel ("key", "value") 
        }
        let result = theService.ToResource()

        test <@ result.spec.selector.Value["key"] = "value" @>
