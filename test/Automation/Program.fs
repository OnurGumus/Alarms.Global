module HolidayTracker.Automation.Program

open System.Reflection
open TickSpec

[<EntryPointAttribute>]
let main _ =
    try
        try
            do
                let ass = Assembly.GetExecutingAssembly()
                let definitions = StepDefinitions(ass)

                [ "Subscribe"; "Authenticate" ]
                |> Seq.iter (fun source ->
                    let stream = ass.GetManifestResourceStream("Automation." + source + ".feature")
                    definitions.Execute(source, stream))
            0
        with e ->
            printf "%A" e
            -1
    finally
        Setup.host.StopAsync().Wait()