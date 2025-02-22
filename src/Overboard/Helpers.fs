﻿namespace Overboard

module Result =

    let isOk = function | Ok _ -> true | Error _ -> false
    let isError = function | Ok _ -> false | Error _ -> true
    let unwrap = function | Ok v -> v | Error err -> failwithf "%A" err

module Helpers =
    open System
    open System.Collections.Generic

    let nullableOfOption = function
        | None -> new Nullable<_>()
        | Some x -> new Nullable<_>(x)

    let mapValues f = function
        | [] -> None
        | xs -> Some (f xs)

    let mapEach f = function
        | [] -> None
        | xs -> Some (List.map f xs)
    
    let listToDict ss =
        match ss with
        | xs when Seq.isEmpty xs -> None
        | xs -> Some (dict xs)

    let mapToIDictionary (map) = map :> System.Collections.Generic.IDictionary<_,_>

    let emptyAsNone (ss) =
        if Seq.isEmpty ss then None
        else Some ss

    let mergeInt32 v1 v2 =
        match (v1,v2) with
        | v1, 0 -> v1
        | 0, v2 -> v2
        | _ -> v1

    let mergeInt64 v1 v2 =
        match (v1,v2) with
        | v1, 0L -> v1
        | 0L, v2 -> v2
        | _ -> v1

    let mergeString v1 v2 =
        match (v1,v2) with
        | v1, "" -> v1
        | "", v2 -> v2
        | _ -> v1

    let mergeBool v1 v2 = 
        match (v1,v2) with
        | v1, false -> v1
        | false, v2 -> v2
        | _ -> v1
    
    let mergeOption v1 v2 =
        match (v1,v2) with
        | Some v1', None -> v1
        | None, Some v2' -> v2
        | None, None -> None
        | _ -> v1

    let mergeMap (m1: Map<string,'a>) (m2: Map<string,'a>) = 
        let getValue k =
            if Map.containsKey k m1 then m1[k]
            else m2[k]
        let allKeys: string seq = Seq.append (Map.keys m1) (Map.keys m2)
        allKeys
        |> Seq.distinct
        |> Seq.map (fun key -> key,getValue key)
        |> Map.ofSeq


module String =
    open System

    let lower (s: string) = s.ToLower()
    let length (s: string) = s.Length
    let isEmpty (s: string) = String.IsNullOrEmpty s
    let stripSpaces (s: string) = s.Replace(" ", "")
    let toBase64 (bytes: byte[]) = Convert.ToBase64String(bytes)
    let fromBase64 (s: string) = Convert.FromBase64String(s)
    let isBase64 (s) =
        if ((s |> stripSpaces |> length) % 4) = 0 then true
        else
            try
                fromBase64 s |> ignore
                true
            with
            | _ -> false
    let toOption (s: string) = if String.IsNullOrWhiteSpace s then None else Some s

module Files =
    open System
    let ext (path: string) = IO.Path.GetExtension(path)
    let fileNameWithoutExt (path: string) = IO.Path.GetFileNameWithoutExtension(path)
    let fileName (path: string) = IO.Path.GetFileName(path)
    let isFsxFile (path: string) = IO.Path.GetExtension(path).ToLower() = "fsx" 