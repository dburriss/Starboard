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

type MnMBuilder() =
    member _.Yield(o) = MnM.empty

    member _.Run(state: MnM) = 
        // run validation?
        state
    
    [<CustomOperation "add_person">]
    member _.AddPerson(state: MnM, person) = 
        { state with people = state.people @ [person] }
    
    [<CustomOperation "add_machine">]
    member _.AddMachine(state: MnM, machine) = 
        { state with machines = state.machines @ [machine] }

let mnm = new MnMBuilder()

let m = mnm {
    add_machine m1
}