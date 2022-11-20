namespace Overboard.Acceptance.Tests

module K8s_CronJob =

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
    let ``kind is CronJob`` () =
        
        let sut = cronJob {
            _name "my-name"
        }

        let resource = sut.ToResource() 
        test <@ resource.kind = "CronJob" @>

    [<Fact>]
    let ``apiVersion is batch v1`` () =
        
        let sut = cronJob {
            _name "my-name"
        }

        let resource = sut.ToResource() 
        test <@ resource.apiVersion = "batch/v1" @>
        
    [<Fact>]
    let ``metadata name is set`` () =
        
        let sut = cronJob {
            _name "my-cron-job"
        }

        let resource = sut.ToResource() 
        test <@ resource.metadata.name.Value = "my-cron-job" @>

    [<Fact>]
    let ``defaults are set`` () =
        
        let sut = cronJob {
            _name "my-cron-job"
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.schedule.IsNone @>
        test <@ resource.spec.timeZone = "Etc/UTC" @>
        test <@ resource.spec.concurrencyPolicy = "Allow" @>
        test <@ resource.spec.startingDeadlineSeconds.IsNone @>
        test <@ resource.spec.suspend = false @>
        test <@ resource.spec.successfulJobsHistoryLimit = 3 @>
        test <@ resource.spec.failedJobsHistoryLimit = 1 @>
        
    [<Fact>]
    let ``values are set`` () =
        
        let sut = cronJob {
            _name "my-cron-job"
            jobTemplate aJob
            schedule "0 * * * *"
            timeZone "Europe/Brussels"
            concurrencyPolicy Replace
            startingDeadlineSeconds 10L
            suspend
            successfulJobsHistoryLimit 99
            failedJobsHistoryLimit 7
        }

        let resource = sut.ToResource() 
        test <@ resource.spec.schedule = Some  "0 * * * *" @>
        test <@ resource.spec.timeZone = "Europe/Brussels" @>
        test <@ resource.spec.concurrencyPolicy = "Replace" @>
        test <@ resource.spec.startingDeadlineSeconds = Some 10L @>
        test <@ resource.spec.suspend = true @>
        test <@ resource.spec.successfulJobsHistoryLimit = 99 @>
        test <@ resource.spec.failedJobsHistoryLimit = 7 @>
    [<Fact>]
    let ``does not validate without a job`` () =
        
        let sut = cronJob {
            _name "my-cron-job"
            schedule "0 * * * *"
        }

        test <@ sut.Validate().IsEmpty = false @>
        
    [<Fact>]
    let ``does not validate without a schedule`` () =
        
        let sut = cronJob {
            _name "my-cron-job"
            jobTemplate aJob
        }

        test <@ sut.Validate().IsEmpty = false @>
        
    [<Fact>]
    let ``validates with job required set`` () =
        
        let sut = cronJob {
            _name "my-cron-job"
            jobTemplate aJob
            schedule "0 * * * *"
        }

        test <@ sut.Validate().IsEmpty = true @>               
      
    
