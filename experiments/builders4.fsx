type Person = {
    age: int
    name: string
}
type Person with
    static member empty = {
        age = 0
        name = ""
    }

type PersonBuilder() =
    member _.Yield(_) = Person.empty

    member _.Run(state: Person) = 
        // run validation?
        state
    
    /// Sets the name of the person.
    [<CustomOperation "name">]
    member _.Name(state: Person, name) = { state with name = name }
    
    /// Sets the name of the person.
    [<CustomOperation "age">]
    member _.Age(state: Person, age) = { state with age = age }

let person = new PersonBuilder()

type Machine = {
    watts: int
    purpose: string
}
type Machine with
    static member empty = {
        watts = 0
        purpose = "paper_weight"
    }

type MachineBuilder() =
    member _.Yield(_) = Machine.empty

    member _.Run(state: Machine) = 
        // run validation?
        state
    
    [<CustomOperation "purpose">]
    member _.Purpose(state: Machine, purpose) = { state with purpose = purpose }
    
    [<CustomOperation "watts">]
    member _.Watts(state: Machine, watts) = { state with watts = watts }

let machine = new MachineBuilder()


type MnM = {
    name: string
    people: Person list
    machines: Machine list
}
type MnM with
    static member empty = {
        name = ""
        people = List.empty
        machines = List.empty
    }

type MnMBuilder() =
    
    member __.Zero () = MnM.empty
    member inline _.Yield(x: unit) = 
        printfn "Yield empty"
        MnM.empty

    member inline _.Yield(machine: Machine) = 
        printfn "Yield machine"
        { MnM.empty with machines = [machine] }
        
    member inline _.Yield(machines: Machine list) = 
        printfn "Yield machines"
        { MnM.empty with machines = machines }

    member inline _.YieldFrom(machines: Machine list) = 
        printfn "YieldFrom machine"
        { MnM.empty with machines = machines }

    member inline _.Yield(person: Person) = 
        printfn "Yield person"
        { MnM.empty with people = [person] }
    
    member __.Combine (currentValueFromYield, accumulatorFromDelay) =
        printfn "current: %A" currentValueFromYield
        printfn "delayVal: %A" accumulatorFromDelay

        let combine v1 v2 =
            match (v1,v2) with
            | v1, "" -> v1
            | "", v2 -> v2
            | _ -> failwithf "Value set twice %s %s" v1 v2 

        { currentValueFromYield with 
            name  = combine (currentValueFromYield.name) (accumulatorFromDelay.name)
            machines = currentValueFromYield.machines @ accumulatorFromDelay.machines;
            people = currentValueFromYield.people @ accumulatorFromDelay.people }

    member __.Delay f = 
        printfn "Start Delay"
        let value = f()
        printfn "- Delay value"
        printfn "- %A" value
        value
    // member _.Run(f) : MnM = 
    //     // run validation?
    //     f(MnM.empty)

    member this.For(state: MnM , f: unit -> MnM) =
        printfn "For"
        printfn "- State"
        let delayed= f()
        printfn "- %A" delayed
        this.Combine(state, delayed)
      
    [<CustomOperation "name">]
    member __.Name(state: MnM, name) = 
        { state with name = name }

    [<CustomOperation "add_person">]
    member __.AddPerson(state: MnM, person) = 
        { state with people = state.people @ [person] }
    
    [<CustomOperation "add_machine">]
    member __.AddMachine(machine) = 
        fun (state: MnM) -> { state with machines = state.machines @ [machine] }
    
    
let mnm = new MnMBuilder()

let person1 = person {
    name "Last man alive"
}
let machine1 = machine {
    purpose "Terminator"
    watts System.Int32.MaxValue
}

let moreMachines = [
    machine {
        purpose "Objective 1"
    }
    machine {
        purpose "Objective 2"
    }
]

let xs = seq {
    1
    2
    3
    4
}

let endState = mnm {
    name "Catalog"
    person { 
        name "Adam"
    }
    machine {
        purpose "Hunter"
        watts 100
    }
    person1
    add_person person1
    person1;person1
    [machine1]
    machine1
    moreMachines
}


printfn "End state of %s" (endState.name)
printfn "People %i" (List.length endState.people)
printfn "Machines %i" (List.length endState.machines)
printfn "%A" endState
