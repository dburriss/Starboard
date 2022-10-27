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

let machine1 = machine {
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

type MnMBuilder() =

    member __.Zero () = MnM.empty
    member inline _.Yield(machine: Machine) = fun (state: MnM) -> { state with machines = state.machines @ [machine] }
    member inline _.Yield(person: Person) = fun (state: MnM) -> { state with people = state.people @ [person] }
    // member inline this.Yield (things: #seq<_>) =
    //     Seq.head things
    //         // let fs = seq {
    //         //     for thing in things do 
    //         //         match box(thing) with
    //         //         | :? Machine as x -> this.Yield(x)
    //         //         | :? Person as x -> this.Yield(x)
    //         //         | _ -> failwithf "Unknown type for Yield %s" (thing.GetType().Name)
    //         // }
    //         // fun (state: MnM) -> 
    //         //     fs |> List.reduce (fun s  -> )
    //         //     // List.fold (fun args f -> f args) (CSIDriverArgs()) args
                    
                
    //member inline __.YieldFrom (f: MnMMutation) = f
    member __.Combine (f, g) = fun (state: MnM) -> g(f state)
    member __.Delay f = fun (state: MnM) -> (f()) state
    member _.Run(f: MnMMutation) = 
        // run validation?
        f(MnM.empty)
    
    [<CustomOperation "add_person">]
    member _.AddPerson(person) = 
        fun (state: MnM) -> { state with people = state.people @ [person] }
    
    [<CustomOperation "add_machine">]
    member _.AddMachine(machine) = 
        fun (state: MnM) -> { state with machines = state.machines @ [machine] }
    
    
let mnm = new MnMBuilder()

let person1 = person {
    name ""
}

let m2 = mnm {
    person { 
            name ""
        }
    machine {
        purpose "Recon"
    }
}


printfn "%A" m2