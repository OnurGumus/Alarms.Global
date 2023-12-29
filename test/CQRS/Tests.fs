module Tests

open Expecto
open ExpectoTickSpecHelper
open Serilog
open Serilog.Sinks.SystemConsole.Themes

Log.Logger <-
    LoggerConfiguration()
        .MinimumLevel.Debug()
        .Destructure.FSharpTypes()
        .WriteTo.Console(theme = AnsiConsoleTheme.Literate, applyThemeToRedirectedOutput = true)
        .Enrich.FromLogContext()
        .CreateLogger()

[<Tests>]
let global_event = featureTest "GlobalEvent.feature"