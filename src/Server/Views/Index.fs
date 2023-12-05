module HolidayTracker.Server.Views.Index

open Common
open Thoth.Json.Net
open Microsoft.AspNetCore.Http
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model
open System

let view (env: _) (ctx: HttpContext) (dataLevel: int) =
    task {
        let query = env :> IQuery
        let! countries = query.Query<Country>()
        let user = ctx.User.Identity.Name

        let countryNames =
            countries
            |> List.map (fun country ->

                html
                    $"""
                <li>
                    <label class="gui-switch">
                        {country.Name.Value}
                        <input is="gui-switch"
                            data-id={country.CountryId.Value}
                            data-name={country.Name.Value}
                            class="country-selector" type="checkbox" role="switch">
                    </label>
                </li>""")
            |> String.concat Environment.NewLine
        let siginIn = 
            match user with
            | null | "" -> html $"<ht-signin></ht-signin>"
            | _ ->  html $"<ht-signin username={user}></ht-signin>"
            
        return
            html
                $""" 
            {siginIn}
            <ht-countries-selector>
                <h{dataLevel + 1}> Countries </h{dataLevel + 1}>
                <fieldset>
                    <legend>Choose countries to subscribe</legend>
                    <ul>
                        {countryNames}
                    </ul>
                </fieldset>
            </ht-countries-selector>
        """
    }
