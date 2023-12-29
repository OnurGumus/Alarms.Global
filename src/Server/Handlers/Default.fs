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

    let auth =
        requiresAuthentication (challenge CookieAuthenticationDefaults.AuthenticationScheme)

    choose [
        routeCi "/" >=> defaultRoute
        routeCi "/privacy" >=> viewRoute (Privacy.view env)
        POST >=> route "/signin-google" >=> (googleSignIn env)
#if DEBUG
        POST >=> route "/signin-test" >=> (testSignIn env)
#endif
        POST >=> route "/sign-out" >=> (signOut env)

        routex @".*(?:Subscribe|Unsubscribe).*"
        >=> auth
        >=> (Subscription.subscriptionHandler env)
        Admin.handler env layout
    ]

let webAppWrapper (env: _) (layout: HttpContext -> (int -> Task<string>) -> string Task) =
    fun (next: HttpFunc) (context: HttpContext) -> task { return! webApp env layout next context }
