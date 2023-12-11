module HolidayTracker.Server.Environments

open Microsoft.Extensions.Configuration
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model

[<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
type AppEnv(config: IConfiguration) =

    let mutable queryApi = Unchecked.defaultof<_>

    interface IConfiguration with
        member _.Item
            with get (key: string) = config.[key]
            and set key v = config.[key] <- v

        member _.GetChildren() = config.GetChildren()
        member _.GetReloadToken() = config.GetReloadToken()
        member _.GetSection key = config.GetSection(key)

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

    member _.Reset() = ()

    member _.Init() =
        DB.init config
        queryApi <- (HolidayTracker.Query.API.api config null)
