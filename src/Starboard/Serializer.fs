namespace Starboard.Serialization

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