namespace Overboard.Building.Tests

module ValidatorTests =

    open System
    open Xunit
    open Overboard

    type TestRecord = {
        opti: int option
        opts: string option
        number: int
        name: string
        uri: Uri
        numbers: int list
    }

    let allHere = {
        opti = Some 1
        opts = Some "one"
        number = 1
        name = "one"
        uri = Uri("http://domain.name")
        numbers = [1]
    }

    let noneHere = {
        opti = None
        opts = None
        number = 0
        name = ""
        uri = null
        numbers = []
    }

    //[<Fact>]
    //let ``Property name is opti`` () =
    //    let result = Validation.propertyName <@ (fun x -> x.opti) @>
    //    Assert.Equal("opti", result)


    // required

    [<Fact>]
    let ``Required has no errors on Some`` () =
        let result = Validation.required (fun x -> x.opti) "" allHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``Required has no errors on number`` () =
        let result = Validation.required (fun x -> x.number) "" allHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``Required has no errors on string`` () =
        let result = Validation.required (fun x -> x.name) "" allHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``Required has no errors on object`` () =
        let result = Validation.required (fun x -> x.uri) "" allHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``Required has no errors on empty string`` () =
        let result = Validation.required (fun x -> x.name) "" noneHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``Required is Error on None`` () =
        let result = Validation.required (fun x -> x.opti) "" noneHere
        Assert.False(List.isEmpty result)

    [<Fact>]
    let ``Required is Error on null`` () =
        let result = Validation.required (fun x -> x.uri) "" noneHere
        Assert.False(List.isEmpty result)

    // notEmpty
    let getMessage = function 
        | RequiredMemberIsMissing p -> p
        | InvalidValue p -> p
    
    [<Fact>]
    let ``notEmpty has no errors if not empty string`` () =
        let msg =  "name cannot be empty"
        let result = Validation.notEmpty (fun x -> x.name) msg allHere
        Assert.True(List.isEmpty result)
        
    [<Fact>]
    let ``notEmpty has no errors if not empty list`` () =
        let msg =  "numbers cannot be empty"
        let result = Validation.notEmpty (fun x -> x.numbers) msg allHere
        Assert.True(List.isEmpty result)
            
    [<Fact>]
    let ``notEmpty is Error if empty string`` () =
        let msg =  "name cannot be empty"
        let result = Validation.notEmpty (fun x -> x.name) msg noneHere
        Assert.False(List.isEmpty result)
        let resultMessage = result |> List.map getMessage |> List.head
        Assert.Equal(msg, resultMessage)
            
    [<Fact>]
    let ``notEmpty is Error if empty array`` () =
        let msg =  "numbers cannot be empty"
        let result = Validation.notEmpty (fun x -> x.numbers) msg noneHere
        Assert.False(List.isEmpty result)
        let resultMessage = result |> List.map getMessage |> List.head
        Assert.Equal(msg, resultMessage)
            
    [<Fact>]
    let ``notEmpty has no errors on null`` () =
        let msg =  ""
        let result = Validation.notEmpty (fun x -> x.uri) msg noneHere
        Assert.True(List.isEmpty result)
            
    [<Fact>]
    let ``startsWith has no errors on null`` () =
        let msg =  ""
        let result = Validation.startsWith "http" (fun x -> x.uri) msg noneHere
        Assert.True(List.isEmpty result)
            
    [<Fact>]
    let ``startsWith has no errors on None`` () =
        let msg =  ""
        let result = Validation.startsWith "o" (fun x -> x.opts) msg noneHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``startsWith has no errors if does start with on string option`` () =
        let msg =  ""
        let result = Validation.startsWith "o" (fun x -> x.opts) msg allHere
        Assert.True(List.isEmpty result)

    [<Fact>]
    let ``startsWith has no errors if does start with on string`` () =
        let msg =  ""
        let result = Validation.startsWith "o" (fun x -> x.name) msg allHere
        Assert.True(List.isEmpty result)

            
