open NJsonSchema
open NJsonSchema.CodeGeneration.CSharp
open System
open System.Text
open System.IO
open Starboard.Dtos

let replaceDotWithUnderscore (s: string) = s.Replace(".","_")

let generateDtos schemaName schemaUrl tech version =
    let transformedVersion = replaceDotWithUnderscore version
    let path = Path.Combine(Helper.GetThisFilePath(), $"../../../Starboard.Dtos/{schemaName}_{transformedVersion}.cs")
    task {
        let! schemaFromFile = JsonSchema.FromUrlAsync(schemaUrl)
        let settings = CSharpGeneratorSettings()
        settings.ClassStyle <- CSharpClassStyle.Poco
        settings.Namespace <- $"Starboard.Dtos.{tech}.{transformedVersion}"
        let classGen = CSharpGenerator(schemaFromFile, settings)
        let code = classGen.GenerateFile()
        let sb = new StringBuilder()
        do sb.AppendLine(code) |> ignore
        return File.WriteAllTextAsync(path, sb.ToString())
    } |> fun t -> t.Wait()
    

[<EntryPoint>]
let main args =
    do generateDtos "DevelopmentApps" "https://kubernetesjsonschema.dev/v1.14.0/deployment-apps-v1.json" "Kubernetes" "v1.14.0"
    printfn "Done."
    Console.Read() |> ignore
    0