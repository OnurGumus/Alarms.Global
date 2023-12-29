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
open HolidayTracker.ServerInterfaces.Command
open HolidayTracker.Shared.Model.Authentication
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model.Subscription
open System


let configBuilder = ConfigurationBuilder()
let config = configBuilder.AddHoconFile("test-config.hocon").Build()
let env = AppEnv(config)
env.Init()

[<BeforeScenario>]
let ``setup`` () =
    env.Reset()
    env

[<Given>]
let ``today is (.*)`` (date: string) = printfn "today is %s" date

[<Given>]
let ``we have the following subscribers`` (table: Table, env: AppEnv) =
    let auth = env :> IAuthentication
    let query = env :> IQuery
    let subs = env :> ISubscription
    let allRegions = query.Query<Region>() |> Async.RunSynchronously

    let getUser (email: Email) =
        async {
            let! user = query.Query<User>(filter = Predicate.Equal("ClientId", email.Value), take = 1)
            return user |> Seq.tryHead
        }

    for row in table.Rows do
        let email = row[0] |> UserClientId.TryCreate |> forceValidate

        let user =
            async {
                let cid = CID.CreateNew()
                let _, w = query.Subscribe((fun e -> e.CID = cid), 1, ignore)
                let! _ = auth.IdentifyUser cid email
                do! w
                let! user = getUser (email)
                return user.Value
            }
            |> Async.RunSynchronously

        printfn "user %A" user

        let regions =
            row[1].Split(",")
            |> Array.map (fun s -> s.Trim())
            |> Array.map (fun s -> allRegions |> List.find (fun r -> r.Name.Value = s))

        printfn "regions %A" regions

        for region in regions do
            async {
                let cid = CID.CreateNew()
                let _, w = query.Subscribe((fun e -> e.CID = cid), 1, ignore)
                let! _ = (subs.Subscribe cid (Some user.Identity) region.RegionId)
                do! w
                return ()
            }
            |> Async.RunSynchronously

[<When>]
let ``I publish an event for (.*) (.*)`` (region: string) (date: string) (env: AppEnv) =
    (task {
        let query = env :> IQuery
        let subs = env :> ISubscription
        let allRegions = query.Query<Region>() |> Async.RunSynchronously
        let date = DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc)
        let targetRegion = allRegions |> List.find (fun r -> r.Name.Value = region)
        let cid = CID.CreateNew()

        let globalEvent =
            { GlobalEventId = GlobalEventId.CreateNew()
              Title = "My event" |> ShortString.TryCreate |> forceValidate
              Body = "My Body" |> LongString.TryCreate |> forceValidate
              EventDateInUTC = Some date
              TargetRegion = [ targetRegion.RegionId ]
            }

        let! res = subs.PublishEvent (cid) globalEvent
        printfn "%A" res
        return ()
    })
        .Wait()

[<Then>]
let ``nothing should happen`` () = printfn "nothing should happen"

[<When>]
let ``date becomes (.*)`` (date: string) = printfn "date becomes %s" date

[<Then>]
let ``(.*) should get a notification`` (email: string) =
    printfn "%s should get notification" email
