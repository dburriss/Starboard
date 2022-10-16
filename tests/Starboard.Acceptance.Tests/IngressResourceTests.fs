namespace Starboard.Acceptance.Tests
// Acceptance tests for Ingress
// Question: Does the builder produce only valid K8s Ingress Resource?
// How: Use builder and verify meets Ingress spec (https://kubernetes.io/docs/reference/kubernetes-api/service-resources/ingress-v1/)

module K8s_Ingress =

    open Xunit
    open Starboard
    open Starboard.Resources
    open Swensen.Unquote

    let defaultBackend_serviceName = "my-service-name"
    let defaultBackend_portName = "my-port-name"
    let aClassName = "my-class-name"
    let aSecretName = "my-secret"
    let aBackend = ingressBackend {
        serviceName defaultBackend_serviceName
        servicePortName defaultBackend_portName
    }
    let aHttpPath = httpPath {
        backend aBackend
        path "/"
        pathType IngressPathType.Prefix
    }
    let aHost = "foo://example.com:8042"
    let aRule = 
        ingressRule {
            host aHost
            httpPaths [ aHttpPath ]
        }
    let aTLS = ingressTLS {
        hosts [ aHost ]
        secretName aSecretName
    }
    
    let servicePortAsString = function | Name x -> x | Number x -> x |> string

    [<Fact>]
    let ``kind is Ingress`` () =
        let theIngress = ingress {
            defaultBackend aBackend
        }
        let result = theIngress.ToResource()

        test <@ result.kind = "Ingress" @>
        
    [<Fact>]
    let ``version is networking k8s io v1`` () =
        let theIngress = ingress {
            defaultBackend aBackend
        }
        let result = theIngress.ToResource()

        test <@ result.apiVersion = "networking.k8s.io/v1" @>

    [<Fact>]
    let ``defaultBackend matches input`` () =
        let theIngress = ingress {
            defaultBackend aBackend
        }
        let spec = theIngress.ToResource() |> fun r -> r.spec

        test <@ spec.defaultBackend.Value.service.Value.name.Value = defaultBackend_serviceName @>
        test <@ spec.defaultBackend.Value.service.Value.port.Value.name.Value = defaultBackend_portName @>
      
    [<Fact>]
    let ``ingressClassName is set`` () =
        let theIngress = ingress {
            defaultBackend aBackend
            ingressClassName aClassName
        }
        let spec = theIngress.ToResource() |>  fun r -> r.spec

        test <@ spec.ingressClassName.Value = aClassName @>
              
    [<Fact>]
    let ``rules matches input`` () =
        let theIngress = ingress {
            defaultBackend aBackend
            rules [ aRule ]
        }
        let spec = theIngress.ToResource() |> fun r -> r.spec
        let rule = spec.rules.Value[0]

        test <@ rule.host.Value = aHost @>
        test <@ rule.http.paths.Value.Length = 1 @>
        test <@ rule.http.paths.Value[0].backend.service.Value.name = aBackend.serviceName @>
        test <@ rule.http.paths.Value[0].backend.service.Value.port.Value.name.Value = defaultBackend_portName @>
        test <@ rule.http.paths.Value[0].pathType = "Prefix" @>
        test <@ rule.http.paths.Value[0].path.Value = "/" @>
               
    [<Fact>]
    let ``TLS matches input`` () =
        let theIngress = ingress {
            defaultBackend aBackend
            tls [ aTLS ]
        }
        let spec = theIngress.ToResource() |> fun r -> r.spec
        let tls = spec.tls.Value[0]

        test <@ tls.hosts.Value[0] = aHost @>
        test <@ tls.secretName.Value = aSecretName @>
                
    [<Fact>]
    let ``defaultBackend required if no rules`` () =
        let theIngress = ingress {
            rules []
        }
        let result = theIngress.Valdidate()

        test <@ result = [ValidationProblem.RequiredMemberIsMissing "Ingress `defaultBackend` is required if no `rules` are specified."] @>
