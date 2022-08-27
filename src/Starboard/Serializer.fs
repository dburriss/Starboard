namespace Starboard.Serialization

open Newtonsoft.Json.Linq


module Serializer =

    open System.Text.Json
    open System.Text.Encodings.Web
    open System.Text.Json.Serialization

    let jsonSerializerOptions =
        JsonSerializerOptions(
            WriteIndented = true,
            DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        )

    let toJson x =
        JsonSerializer.Serialize(x, jsonSerializerOptions)

    let ofJson<'T> (x: string) =
        JsonSerializer.Deserialize<'T>(x, jsonSerializerOptions)

    let rec private toObj (jToken: JToken) =
        match jToken with
        | :? JValue as t -> t.Value
        | :? JArray as t -> t.AsJEnumerable() |> Seq.map toObj |> box
        | :? JObject as t -> t.Properties() |> Seq.map (fun p -> (p.Name, (toObj p.Value))) |> dict |> box
        | _ -> failwithf "Unexpected token %s" (jToken.ToString())
        
    let private serializer = YamlDotNet.Serialization.SerializerBuilder().Build()
    let toYaml x = 
        let json = toJson x
        let jToken = JToken.Parse(json)
        let o = toObj jToken
        serializer.Serialize(o)