module HolidayTracker.Test.CQRS.GlobalEvent

open TickSpec
open HolidayTracker.Command.API
open HolidayTracker.Shared.Model
open System.Threading
open HolidayTracker.Query
open System.IO
open Akkling
open Expecto
open Microsoft.Extensions.Configuration
open Hocon.Extensions.Configuration
open Akkling.Streams
open HolidayTracker.Server.Environments

let configBuilder = ConfigurationBuilder()
let config = configBuilder.AddHoconFile("test-config.hocon").Build()
let env = AppEnv(config)
env.Init()

[<BeforeScenario>]
let ``setup`` () =
    env

[<Given>]
let ``today is (.*)`` (date: string) = 
    printfn "today is %s" date

[<Given>]
let ``we have the following subscribers`` (table: Table) = 
    printfn "subscribers %A" table

[<When>]
let ``I publish an event for (.*) (.*)`` (region: string) (date:string) = 
    printfn "publish for %s %s" region date

[<Then>]
let ``nothing should happen`` () = 
    printfn "nothing should happen"

[<When>]
let ``date becomes (.*)`` (date: string) = 
    printfn "date becomes %s" date

[<Then>]
let ``(.*) should get a notification`` (email: string) = 
    printfn "%s should get notification" email
