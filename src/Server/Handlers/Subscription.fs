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
                let cid = CID.CreateNew()
                let query = env :> IQuery
                Log.Information("{@User} has subscribed to {@Region}", ctx.User.Identity.Name, region)
                let subs = env :> ISubscription

                let identity =
                    ctx.User.FindFirst(fun x -> x.Type = Constants.UserIdentity).Value
                    |> UserIdentity.Create

                let _, w = query.Subscribe((fun e -> e.CID = cid), 1, ignore)
                let! res = subs.Subscribe cid (Some identity) region
                do! w
                return res
            }
    Unsubscribe =
        fun _ region ->
            async {
                let cid = CID.CreateNew()
                let query = env :> IQuery
                Log.Information("{@User} has unsubscribed to {@Region}", ctx.User.Identity.Name, region)
                let subs = env :> ISubscription

                let identity =
                    ctx.User.FindFirst(fun x -> x.Type = Constants.UserIdentity).Value
                    |> UserIdentity.Create

                let _, w = query.Subscribe((fun e -> e.CID = cid), 1, ignore)
                let! res = subs.Unsubscribe cid (Some identity) region
                do! w
                return res
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
