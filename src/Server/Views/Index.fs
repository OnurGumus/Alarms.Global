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

        let countryNames =
            countries
            |> List.map (fun country -> 

                html $"""
                <li>
                    <label class="gui-switch">
                        {country.Name.Value}
                        <input is="gui-switch" type="checkbox" role="switch">
                    </label>
                </li>""")
            |> String.concat Environment.NewLine

        return
            html
                $""" 
            <button type=button> Sign In</button>
            <h{dataLevel + 1}> Countries </h{dataLevel + 1}>
            <fieldset>
                <legend> Choose countries to subscribe </legend>
                <ul>
                    {countryNames}
                </ul>
            </fieldset>
        """
    }
