module HolidayTracker.Server.Environments

open Microsoft.Extensions.Configuration
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model

[<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
type AppEnv(config: IConfiguration) =

    interface IConfiguration with
        member _.Item
            with get (key: string) = config.[key]
            and set key v = config.[key] <- v

        member _.GetChildren() = config.GetChildren()
        member _.GetReloadToken() = config.GetReloadToken()
        member _.GetSection key = config.GetSection(key)

    interface IQuery with
        member _.Query<'t>(?filter, ?orderby, ?orderbydesc, ?thenby, ?thenbydesc, ?take, ?skip) =
            let res =
                if typeof<'t> = typeof<Region> then
                    [
                        "Argentina"
                        "Brazil"
                        "Canada"
                        "Denmark"
                        "France"
                    ]|>
                    List.map(fun name -> 
                    {
                        RegionId = RegionId.CreateNew() 
                        AlrernateNames = []
                        RegionType = Country
                        Name = name |> ShortString.TryCreate |> forceValidate
                    })
                    |> List.ofSeq |> box

                else
                    failwith "Not implemented"

            async { return res :?> list<'t> }
    member _.Reset() = ()
