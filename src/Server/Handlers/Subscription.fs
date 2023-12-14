module HolidayTracker.Server.Handlers.Subscription

open Microsoft.AspNetCore.Http
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open HolidayTracker.Shared
open Microsoft.Extensions.Configuration
open HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model
open System.Collections.Generic
open Serilog
open HolidayTracker.ServerInterfaces.Command
open HolidayTracker.Shared.Model.Authentication

let subscriptionAPI (ctx: HttpContext) (env: _) : API.Subscription = {
    Subscribe =
        fun _ region ->
            async {
                Log.Information("{@User} has subscribed to {@Region}", ctx.User.Identity.Name, region)
                let subs = env :> ISubscription

                let identity =
                    ctx.User.FindFirst(fun x -> x.Type = Constants.UserIdentity).Value
                    |> UserIdentity.Create

                return! subs.Subscribe (Some identity) region
            }
    Unsubscribe =
        fun _ region ->
            async {
                Log.Information("{@User} has unsubscribed to {@Region}", ctx.User.Identity.Name, region)
                let subs = env :> ISubscription

                let identity =
                    ctx.User.FindFirst(fun x -> x.Type = Constants.UserIdentity).Value
                    |> UserIdentity.Create

                return! subs.Unsubscribe (Some identity) region
            }
}

let subscriptionHandler (env: _) =
    Remoting.createApi ()
    |> Remoting.withErrorHandler (fun ex routeInfo ->
        Log.Error(ex, "Remoting error")
        Propagate ex.Message)
    |> Remoting.withRouteBuilder (API.Route.builder None)
    |> Remoting.fromContext (fun ctx -> subscriptionAPI ctx env)
    |> Remoting.buildHttpHandler
