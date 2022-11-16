namespace Overboard.Building.Tests


module CronJobTests =

    open System
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

    let aJob = job {
        "my-job"
        aPodWith aContainer
    }

    [<Fact>]
    let ``yield job`` () =
        
        let sut = cronJob {
            _name "my-name"
            aJob
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.jobTemplate.metadata.Value.name.Value = "my-job" @>
