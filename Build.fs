open Fake.Core
open Fake.IO
open Farmer
open Farmer.Builders

open Helpers

initializeContext ()

let sharedPath = Path.getFullName "src/Shared"
let serverPath = Path.getFullName "src/Server"
let clientPath = Path.getFullName "src/Client"
let deployPath = Path.getFullName "deploy"
let sharedTestsPath = Path.getFullName "tests/Shared"
let serverTestsPath = Path.getFullName "tests/Server"
let clientTestsPath = Path.getFullName "tests/Client"

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    run dotnet "fable clean --yes" clientPath // Delete *.fs.js files created by Fable
)

Target.create "Bundle" (fun _ ->
    [ "server", dotnet $"publish -c Release -o \"{deployPath}\"" serverPath
      "client", dotnet "perla build" clientPath ]
    |> runParallel)

Target.create "Azure" (fun _ ->
    let web =
        webApp {
            name "SafePerla"
            operating_system OS.Windows
            runtime_stack Runtime.DotNet60
            zip_deploy "deploy"
        }

    let deployment =
        arm {
            location Location.WestEurope
            add_resource web
        }

    deployment
    |> Deploy.execute "SafePerla" Deploy.NoParameters
    |> ignore)

Target.create "Run" (fun _ ->
    run dotnet "build" sharedPath

    [ "server", dotnet "watch run" serverPath
      "client", dotnet "perla serve" clientPath ]
    |> runParallel)


Target.create "Format" (fun _ -> run dotnet "fantomas . -r" "src")

open Fake.Core.TargetOperators

let dependencies =
    [ "Clean" ==> "Bundle" ==> "Azure"

      "Clean" ==> "Run" ]

[<EntryPoint>]
let main args = runOrDefault args