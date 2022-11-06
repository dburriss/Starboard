namespace Starboard.Building.Tests


module ClusterRoleBindingTests =

    open Xunit
    open Swensen.Unquote
    open Starboard.Authorization
                                          
    [<Fact>]
    let ``subject is set implicitly`` () =

        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
            subject {
                "my-subject1"
                kind "my-kind"
                apiGroup "my-group"
                ns "my-namespace"
            }
            subject {
                "my-subject2"
                kind "my-kind"
                apiGroup "my-group"
                ns "my-namespace"
            }

        }
        let result = sut.ToResource()
        test <@ result.subjects.IsSome @>
        test <@ result.subjects.Value.Length = 2 @>
        test <@ result.subjects.Value.Head.name = "my-subject1" @>
        test <@ result.subjects.Value.Head.kind = "my-kind" @>
        test <@ result.subjects.Value.Head.apiGroup = Some "my-group" @>
        test <@ result.subjects.Value.Head.ns = Some "my-namespace" @>
