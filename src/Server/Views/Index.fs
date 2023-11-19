module HolidayTracker.Server.Views.Index

open Common
open Thoth.Json.Net
open Microsoft.AspNetCore.Http

let view (env: _) (ctx: HttpContext) (dataLevel: int) =
    task {

        return
            html
                $""" 
            <h1> Alarms Global </h1>
        """
    }
