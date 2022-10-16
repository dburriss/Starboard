namespace Starboard

module Result =

    let isOk = function | Ok _ -> true | Error _ -> false
    let isError = function | Ok _ -> false | Error _ -> true
    let unwrap = function | Ok v -> v | Error err -> failwithf "%A" err

module Helpers =
    open System

    let nullableOfOption = function
        | None -> new Nullable<_>()
        | Some x -> new Nullable<_>(x)

    let mapValues f = function
        | [] -> None
        | xs -> Some (f xs)

    let mapEach f = function
        | [] -> None
        | xs -> Some (List.map f xs)

    let toDict lst = mapValues dict lst


