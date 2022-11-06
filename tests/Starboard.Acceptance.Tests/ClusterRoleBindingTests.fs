namespace Starboard.Acceptance.Tests


module K8s_ClusterRoleBinding =

    open Xunit
    open Starboard.Authorization
    open Swensen.Unquote

    [<Fact>]
    let ``kind is ClusterRoleBinding`` () =
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
        }
        let result = sut.ToResource()

        test <@ result.kind = "ClusterRoleBinding" @>
       
    [<Fact>]
    let ``apiVersion is rbac_authorization_k8s_io_v1`` () =
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
        }
        let result = sut.ToResource()

        test <@ result.apiVersion = "rbac.authorization.k8s.io/v1" @>
              
    [<Fact>]
    let ``name is set in metadata`` () =
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
        }
        let result = sut.ToResource()

        test <@ result.metadata.name = "my-cluster-role-binding" @>
                                
    [<Fact>]
    let ``default imagePullSecrets None`` () =
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
        }
        let result = sut.ToResource()

        test <@ result.roleRef = RoleRef.empty  @>
                                   
    [<Fact>]
    let ``roleRef is set`` () =
        let expected = 
            roleRef {
                name "my-role-ref"
                kind "my-kind"
                apiGroup "my-group"
            }
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
            set_roleRef expected
        }
        let result = sut.ToResource()
        test <@ result.roleRef = expected @>
                                      
    [<Fact>]
    let ``roleRef is set via seq`` () =
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
            roleRef {
                "my-role-ref"
                kind "my-kind"
                apiGroup "my-group"
            }
        }
        let result = sut.ToResource()

        test <@ result.roleRef.name = "my-role-ref" @>
        test <@ result.roleRef.kind = "my-kind" @>
        test <@ result.roleRef.apiGroup = "my-group" @>
                                              
    [<Fact>]
    let ``subjects are set`` () =
        let subjects' = [
            subject {
                "my-subject"
                kind "my-kind"
                apiGroup "my-group"
                ns "my-namespace"
            }
        ]
        let sut = clusterRoleBinding {
            "my-cluster-role-binding"
            subjects subjects'
        }
        let result = sut.ToResource()
        test <@ result.subjects = Some subjects' @>
        test <@ result.subjects.Value.Head.name = "my-subject" @>
        test <@ result.subjects.Value.Head.kind = "my-kind" @>
        test <@ result.subjects.Value.Head.apiGroup = Some "my-group" @>
        test <@ result.subjects.Value.Head.ns = Some "my-namespace" @>
