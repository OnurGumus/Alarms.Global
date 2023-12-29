module HolidayTracker.Server.Handlers.Admin

open HolidayTracker.Server.Views
open System.Security.Claims
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open System.Threading.Tasks
open HolidayTracker.Shared.Model.Authentication
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Antiforgery
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open HolidayTracker.Shared.Model
open HolidayTracker.ServerInterfaces.Query

let accessDenied = setStatusCode 401 >=> text "Access Denied"

let mustBeAdmin =
    authorizeUser (fun u -> u.HasClaim(ClaimTypes.Role, "admin")) accessDenied

let handler (env: #_) (layout: HttpContext -> (int -> Task<string>) -> string Task) =
    let query = env :> IQuery

    let viewRoute view =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let! lay = (layout ctx (view ctx))
                return! htmlString lay next ctx
            }

    let defaultRoute = viewRoute (Admin.Index.view env)

    routex "^.*admin.*$"
    >=> requiresAuthentication (challenge CookieAuthenticationDefaults.AuthenticationScheme)
    >=> routeStartsWith "/admin"
    >=> mustBeAdmin
    >=> choose [ route "/admin" >=> defaultRoute ]
