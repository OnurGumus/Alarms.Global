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

                html $"""<li><sl-switch>{country.Name.Value}</sl-switch></li>""")
            |> String.concat Environment.NewLine

        return
            html
                $""" 
            <h{dataLevel + 1}> Alarms Global </h{dataLevel + 1}>
            <h{dataLevel + 2}> Countries </h{dataLevel + 2}>
            <ul>
                {countryNames}
            </ul>
        """
    }
