module HolidayTracker.Server.Handlers.Default

open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open HolidayTracker.Server.Views
open Microsoft.AspNetCore.Authentication.Cookies
open Authentication

let webApp (env: _) (layout: HttpContext -> (int -> Task<string>) -> string Task) =

    let viewRoute view =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let! lay = (layout ctx (view ctx))
                return! htmlString lay next ctx
            }

    let defaultRoute = viewRoute (Index.view env)

    choose [

        routeCi "/" >=> defaultRoute
        routeCi "/privacy" >=> viewRoute (Privacy.view env)
        POST >=> route "/signin-google" >=> (googleSignIn env)
    ]

let webAppWrapper (env: _) (layout: HttpContext -> (int -> Task<string>) -> string Task) =
    fun (next: HttpFunc) (context: HttpContext) -> task { return! webApp env layout next context }
