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

let m1 = machine {
    purpose "Terminator"
    watts System.Int32.MaxValue
}

type MnM = {
    people: Person list
    machines: Machine list
}
type MnM with
    static member empty = {
        people = List.empty
        machines = List.empty
    }

type MnMMutation = MnM -> MnM
type State = MnMMutation list

let _combine ((rName, rArgs), (lName, lArgs)) =
    (match lName, rName with
        | null, null -> null
        | null, name -> name
        | name, null -> name
        | _ -> failwith "Duplicate name"),
    (List.concat [ lArgs; rArgs ])

type MnMBuilder() =

    member _.Yield(_: unit) = null, [ id ]
    member _.Yield(machine: Machine) = machine, [ id ]
    member _.YieldFrom(machines: Machine seq) = machine, [ id ]

    member _.Run(state, args) =
        List.fold (fun args f -> f args) (state) args

    member this.Combine(args) = _combine args
    member this.For(args, delayedArgs) = this.Combine(args, delayedArgs ())
    member _.Delay(f) = f ()
    member _.Zero _ = ()

    // [<CustomOperation "add_person">]
    // member _.AddPerson(person) = 
    //     fun (state: MnM) -> { state with people = state.people @ [person] }
    
    [<CustomOperation "add_machine">]
    member _.AddMachine((n, args), machine) =
        n,
        List.Cons(
            (fun (state: MnM) -> { state with machines = state.machines @ [machine] }),
            args
        )
    
let mnm = new MnMBuilder() // note this is returning machine not MnM

let m = mnm {
    machine {
        purpose "Killer"
        watts System.Int32.MaxValue
    }
    person {
        name ""
    }
}

printfn "%A" m 