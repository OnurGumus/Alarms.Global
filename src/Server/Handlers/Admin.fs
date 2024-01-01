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
open HolidayTracker.Shared.Model.Subscription
open Serilog
open HolidayTracker.ServerInterfaces.Command

let accessDenied = setStatusCode 401 >=> text "Access Denied"

let mustBeAdmin =
    authorizeUser (fun u -> u.HasClaim(ClaimTypes.Role, "admin")) accessDenied

let handler (env: #_) (layout: HttpContext -> (int -> Task<string>) -> string Task) =
    let query = env :> IQuery
    let subscription = env :> ISubscription

    let viewRoute view =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let! lay = (layout ctx (view ctx))
                return! htmlString lay next ctx
            }

    let defaultRoute = viewRoute (Admin.Index.view env)

    let publishEventPost =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let form = ctx.Request.Form
                let date = form["date"][0]
                let title = form["title"][0]
                let body = form["body"][0]
                let regionNames = form["regions"][0]

                let! regions = query.Query<Region>()

                let findRegionByName name =
                    let r = regions |> List.find (fun r -> r.Name.Value = name)
                    r.RegionId

                let globalEvent: GlobalEvent = {
                    GlobalEventId = GlobalEventId.CreateNew()
                    Title = title |> ShortString.TryCreate |> forceValidate
                    Body = body |> LongString.TryCreate |> forceValidate
                    EventDateInUTC =
                        if date = "" then
                            None
                        else
                            System.DateTime.SpecifyKind(System.DateTime.Parse(date), System.DateTimeKind.Utc)
                            |> Some
                    TargetRegion =
                        regionNames.Split(",")
                        |> Seq.map (fun s -> s.Trim() |> findRegionByName)
                        |> List.ofSeq

                }

                Log.Information("Publishing {@globalEvent}", globalEvent)
                let! _ = subscription.PublishEvent (CID.CreateNew()) globalEvent

                let! lay = layout ctx (Admin.PublishEvent.view)
                return! htmlString lay next ctx

            }

    let publishEventGet =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let! lay = layout ctx (Admin.PublishEvent.view)
                return! htmlString lay next ctx
            }

    routex "^.*admin.*$"
    >=> requiresAuthentication (challenge CookieAuthenticationDefaults.AuthenticationScheme)
    >=> routeStartsWith "/admin"
    >=> mustBeAdmin
    >=> choose [
        route "/admin" >=> defaultRoute
        POST >=> route "/admin/publish-event" >=> publishEventPost
        GET >=> route "/admin/publish-event" >=> publishEventGet
    ]
