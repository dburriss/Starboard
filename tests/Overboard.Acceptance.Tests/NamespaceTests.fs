namespace Overboard.Acceptance.Tests

module K8s_Namespace =

    open Xunit
    open Overboard.Cluster
    open Swensen.Unquote

    [<Fact>]
    let ``kind is Namespace`` () =
        let sut = ns {
            _name "name"
        }
        let result = sut.ToResource()

        test <@ result.kind = "Namespace" @>
        
    [<Fact>]
    let ``version is networking k8s io v1`` () =
        let sut = ns {
            _name "name"
        }
        let result = sut.ToResource()

        test <@ result.apiVersion = "v1" @>

            
    [<Fact>]
    let ``name is set`` () =
        let sut = ns {
            _name "name"
        }
        let result = sut.ToResource()

        test <@ result.metadata.name = Some "name" @>
