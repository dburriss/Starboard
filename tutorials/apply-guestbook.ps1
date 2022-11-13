dotnet fsi $PSScriptRoot\guestbook.fsx | Out-Null
kubectl apply -f "$PSScriptRoot\guestbook.yaml" | Out-Null