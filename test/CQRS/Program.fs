module Program

open Expecto.Tests
open Serilog
open Serilog.Formatting.Compact
open Serilog.Sinks.SystemConsole.Themes

[<EntryPoint>]
let main args =
    Log.Logger <-
        LoggerConfiguration()
            .MinimumLevel.Debug()
            .Destructure.FSharpTypes()
            .WriteTo.Console(theme = AnsiConsoleTheme.Literate, applyThemeToRedirectedOutput = true)
            .Enrich.FromLogContext()
            .CreateLogger()

    runTestsInAssemblyWithCLIArgs [] [| "--sequenced" |]
