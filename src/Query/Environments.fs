module HolidayTracker.Server.Environments

open Microsoft.Extensions.Configuration
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Model.Authentication
open HolidayTracker.ServerInterfaces.Command
open HolidayTracker.Command.API
open Command.Actor

[<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
type AppEnv(config: IConfiguration) as self =

    let mutable queryApi = Unchecked.defaultof<_>
    let mutable commandApi = Unchecked.defaultof<_>

    interface IConfiguration with
        member _.Item
            with get (key: string) = config.[key]
            and set key v = config.[key] <- v

        member _.GetChildren() = config.GetChildren()
        member _.GetReloadToken() = config.GetReloadToken()
        member _.GetSection key = config.GetSection(key)

    interface IAuthentication with
        member _.IdentifyUser cid = commandApi.IdentifyUser cid

    interface ISubscription with
        member _.Subscribe cid = commandApi.Subscribe cid

        member _.Unsubscribe cid = commandApi.Unsubscribe cid
            
        member _.PublishEvent cid = commandApi.PublishEvent cid

    interface IQuery with
        member _.Query(?filter, ?orderby, ?orderbydesc, ?thenby, ?thenbydesc, ?take, ?skip) =
            queryApi.Query(
                ?filter = filter,
                ?orderby = orderby,
                ?orderbydesc = orderbydesc,
                ?thenby = thenby,
                ?thenbydesc = thenbydesc,
                ?take = take,
                ?skip = skip
            )

        member _.Subscribe(cb) = queryApi.Subscribe(cb)
        member _.Subscribe(filter, take, cb) = queryApi.Subscribe(filter, take, cb)

    member this.Reset() =
        DB.reset config
        let commandApi: IAPI = commandApi
        let system = commandApi.ActorApi.System
        system.Terminate().Wait()
        system.WhenTerminated.ContinueWith(fun _ -> this.Init()).Wait()

    member _.Init() =
        DB.init config
        commandApi <- HolidayTracker.Command.API.api self NodaTime.SystemClock.Instance
        queryApi <- (HolidayTracker.Query.API.api config commandApi.ActorApi)
