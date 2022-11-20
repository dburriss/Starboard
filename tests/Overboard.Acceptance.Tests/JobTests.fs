namespace Overboard.Acceptance.Tests

module K8s_Job =

    open Xunit
    open Overboard.Common
    open Overboard.Workload
    open Swensen.Unquote
    
    let aContainer = container {
            name "nginx"
            image "nginx:1.14.2"
            command ["systemctl"]
            args ["config"; "nginx"]
        }

    let aPodWith aContainer = pod {
            add_container aContainer
        }

    [<Fact>]
    let ``kind is Job`` () =
        
        let sut = job {
            _name "my-name"
        }

        let resource = sut.ToResource() 
        test <@ resource.kind = "Job" @>

    [<Fact>]
    let ``apiVersion is batch v1`` () =
        
        let sut = job {
            _name "my-name"
        }

        let resource = sut.ToResource() 
        test <@ resource.apiVersion = "batch/v1" @>
        
    [<Fact>]
    let ``metadata name is set`` () =
        
        let sut = job {
            _name "my-job"
        }

        let resource = sut.ToResource() 
        test <@ resource.metadata.name.Value = "my-job" @>

    [<Fact>]
    let ``replicas fields are set`` () =
        
        let pod1 = pod {
            _name "my-pod"
            add_container aContainer
        }
        let sut = job {
            _name "my-name"
            template pod1
            parallelism 2
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.template.metadata.Value.name.Value = "my-pod" @>
        test <@ resource.spec.parallelism = 2 @>
        
    [<Fact>]
    let ``lifecycle fields defaults are set`` () =

        let sut = job {
            _name "my-name"
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.completions = 1 @>
        test <@ resource.spec.completionMode = "NonIndexed" @>
        test <@ resource.spec.backoffLimit = 6 @>
        test <@ resource.spec.activeDeadlineSeconds = None @>
        test <@ resource.spec.ttlSecondsAfterFinished = 30 @>
        test <@ resource.spec.suspend = false @>
           
    [<Fact>]
    let ``lifecycle fields are set`` () =

        let sut = job {
            _name "my-name"
            completions 99
            completionMode CompletionMode.Indexed
            backoffLimit 101
            activeDeadlineSeconds 102L
            ttlSecondsAfterFinished 103
            suspend
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.completions = 99 @>
        test <@ resource.spec.completionMode = "Indexed" @>
        test <@ resource.spec.backoffLimit = 101 @>
        test <@ resource.spec.activeDeadlineSeconds = Some 102L @>
        test <@ resource.spec.ttlSecondsAfterFinished = 103 @>
        test <@ resource.spec.suspend = true @>
        
    [<Fact>]
    let ``selector defaults fields are set`` () =

        let sut = job {
            _name "my-name"

        }

        let resource = sut.ToResource() 
        test <@ resource.spec.selector = None @>
        test <@ resource.spec.manualSelector = false @>
         
    [<Fact>]
    let ``selector fields are set`` () =

        let sut = job {
            _name "my-name"
            labelSelector {
                matchLabels [("test", "label")]
            }
            manualSelector
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.selector.Value.matchLabels.Value["test"] = "label" @>
        test <@ resource.spec.manualSelector = true @>
                 
    [<Fact>]
    let ``validates with pod`` () =
        let aPod = pod {
            add_container aContainer
            restartPolicy Never
        }
        let sut = job {
            _name "my-name"
            template aPod
        }

        test <@ sut.Validate().IsEmpty = true @>               
    
    [<Fact>]
    let ``does not validate without pod`` () =
        
        let sut = job {
            _name "my-name"
        }

        test <@ sut.Validate().IsEmpty = false @>
        
    [<Fact>]
    let ``restartPolicy can be set to Never`` () =
        let aPod = pod {
            add_container aContainer
            restartPolicy Never
        }
        let sut = job {
            _name "my-name"
            template aPod
        }
        let resource = sut.ToResource() 
        test <@ resource.spec.template.spec.IsSome @>
        test <@ resource.spec.template.spec.Value.restartPolicy = "Never" @>
