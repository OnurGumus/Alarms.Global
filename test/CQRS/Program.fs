module Program

open Expecto.Tests
open Serilog
open Serilog.Formatting.Compact
open Serilog.Sinks.SystemConsole.Themes


[<EntryPoint>]
let main args =
    runTestsInAssemblyWithCLIArgs [] [| "--sequenced" |]
