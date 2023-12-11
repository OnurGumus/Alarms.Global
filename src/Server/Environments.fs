module HolidayTracker.Server.Environments

open Microsoft.Extensions.Configuration
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Model.Authentication
open HolidayTracker.ServerInterfaces.Command

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
            member _.IdentifyUser = 
                commandApi.IdentifyUser

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

    member _.Reset() = ()

    member _.Init() =
        DB.init config
        commandApi <-
            HolidayTracker.Command.API.api self NodaTime.SystemClock.Instance
        queryApi <- (HolidayTracker.Query.API.api config commandApi.ActorApi)
