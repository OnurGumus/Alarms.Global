module HolidayTracker.Server.Views.Index

open Common
open Thoth.Json.Net
open Microsoft.AspNetCore.Http
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model
open System
open Subscription
open HolidayTracker.Shared

let view (env: _) (ctx: HttpContext) (dataLevel: int) =
    task {
        let query = env :> IQuery
        let! countries = query.Query<Region>()
        let user = ctx.User.Identity.Name

        let! subscriptions =
            if ctx.User.Identity.IsAuthenticated then
                let identity = ctx.User.FindFirst(fun x -> x.Type = Constants.UserIdentity).Value
                query.Query<UserSubscription>(filter = Equal("Identity", identity))
            else
                async { return [] }

        let regions = subscriptions |> List.map _.RegionId |> Set.ofList

        let countryNames =
            countries
            |> List.map (fun country ->

                html
                    $"""
                    <label class="setting">
                        <span class="setting__label"> {country.Name.Value}</span>
                        <span class="switch">
                            <input class="switch__input country-selector"
                                data-id={country.RegionId.Value}
                                data-name={country.Name.Value} 
                                {if regions |> Set.contains country.RegionId then
                                     "checked"
                                 else
                                     ""}
                                type="checkbox" role="switch" name="switch1">
                            <span class="switch__fill" aria-hidden="true">
                                <span class="switch__text">ON</span>
                                <span class="switch__text">OFF</span>
                            </span>
                        </span>
                    </label>
                """)
            |> String.concat Environment.NewLine

        let siginIn =
            match user with
            | null
            | "" -> html $"<ht-signin heading-level={dataLevel}></ht-signin>"
            | _ -> html $"<ht-signin heading-level={dataLevel} username={user}></ht-signin>"

        return
            html
                $""" 
            {siginIn}
            <ht-countries-selector>
                <h{dataLevel + 1}> Countries </h{dataLevel + 1}>
                <form>
                    {countryNames}
                </form>
            </ht-countries-selector>
        """
    }
