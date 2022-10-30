namespace Starboard

type ValidationProblem = 
    | RequiredMemberIsMissing of string
    | InvalidValue of string
type ValidationProblem with
    member this.Message =
        match this with
        | RequiredMemberIsMissing msg -> msg
        | InvalidValue msg -> msg

module Validation =

    open System
    open System.Collections
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.Patterns

    let private isSeq (t: Type) = t.IsGenericType && (t.GetGenericTypeDefinition() = typedefof<_ seq>)
    let private isList (t: Type) = t.IsGenericType && (t.GetGenericTypeDefinition() = typedefof<_ list>)
    let private isEnumerable (t: Type) = (typeof<IEnumerable>).IsAssignableFrom(t)

    let getMethodInfo (e : Expr<'T>) : MethodInfo =
          match e with
          | Patterns.Call (_, mi, _) -> mi
          | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

    let (|IsSeq|_|) (o:obj) =
        let t = o.GetType()
        if isSeq t then
            let test = seq {1;2;3}
            let seqT = typedefof<seq<_>>
            let mi = seqT.GetMethod("Length")
            Some 0
        else None
        
    let (|IsList|_|) (o: obj) =
        let t = o.GetType()
        if isList t then
            let listT = typeof<IEnumerable>
            let mi = listT.GetMethod("GetEnumerator")
            let e = mi.Invoke(o, null) :?> IEnumerator
            let isEmpty = not (e.MoveNext())
            Some isEmpty
        else None

    let (|IsEnumerable|_|) (o: obj) =
        let t = o.GetType()
        if isEnumerable t then
            let listT = typeof<IEnumerable>
            let mi = listT.GetMethod("GetEnumerator")
            let e = mi.Invoke(o, null) :?> IEnumerator
            let isEmpty = not (e.MoveNext())
            Some isEmpty
        else None

    let rec private propertyName quotation =
        match quotation with
        | PropertyGet (_,propertyInfo,_) -> propertyInfo.Name
        | Lambda (_,expr) -> propertyName expr
        | _ -> ""

    let required (lense: 'a -> 'b) msg toTest =
        match (lense toTest |> box) with 
        | null -> [(RequiredMemberIsMissing msg)]
        | _ -> []

    let notEmpty (lense: 'a -> 'b) msg toTest =
        let v = lense toTest
        match (box v) with 
        | null -> []
        | IsEnumerable isEmpty when isEmpty -> [(InvalidValue msg)]
        | :? string as s when String.length s = 0 -> [(InvalidValue msg)]
        | _ -> []

    let requiredIfNone requiredProp possibleNone msg toTest =
        match ((requiredProp toTest), (possibleNone toTest)) with
        | None, None -> [RequiredMemberIsMissing msg]
        | _ -> []

    let requiredIfEmpty requiredProp possibleEmpty msg toTest =
        match ((requiredProp toTest), (possibleEmpty toTest)) with
        | None, [] -> [RequiredMemberIsMissing msg]
        | _ -> []

    let startsWith (start: string) (lense: 'a -> 'b) msg toTest =
        let check (s: string) = if s.StartsWith(start) then [] else [InvalidValue(msg)]
        let v = lense toTest
        match (box v) with 
        | null -> []
        | :? Option<string> as opt ->
            match opt with
            | None -> []
            | Some s -> check s   
        | :? string as s -> check s
        | _ -> []