module HolidayTracker.Server.Handlers.Authentication


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
open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Model
open System.IO
open System.Collections.Generic

let prepareClaimsPrincipal name (config: IConfiguration) =
    let admins =
        config.GetSection("config:admins").AsEnumerable()
        |> Seq.map (fun x -> x.Value)
        |> Seq.filter (isNull >> not)
        |> Set.ofSeq

    let claims = [ Claim(ClaimTypes.Name, name) ]

    let claims =
        if admins |> Set.contains name then
            Claim(ClaimTypes.Role, "admin") :: claims
        else
            claims

    ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
    |> ClaimsPrincipal


open Google.Apis.Auth
open Google.Apis.Auth.OAuth2
open Serilog
open HolidayTracker.Shared.Model
open HolidayTracker.ServerInterfaces.Command

let signOut (env: #_) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            do!
                ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                |> Async.AwaitTask

            ctx.SetHttpHeader("Location", "/")
            return! setStatusCode 303 earlyReturn ctx
        }

let googleSignIn (env: _) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            Log.Information("google login start")
            let token = ctx.Request.Form.["credential"][0]
            let csrf = ctx.Request.Form.["g_csrf_token"][0]

            if ctx.Request.Cookies["g_csrf_token"] <> csrf then
                return! setStatusCode 401 earlyReturn ctx
            else
                let! payload = GoogleJsonWebSignature.ValidateAsync(token)
                let config = env :> IConfiguration
                let email = payload.Email
                let name = payload.Name
                let userId = email |> UserClientId.TryCreate |> forceValidate

                let auth = env :> IAuthentication
                let! res = auth.IdentifyUser userId
                Log.Debug("Identification {@res}", res)

                let p = prepareClaimsPrincipal userId.Value config
                let authProps = AuthenticationProperties()
                authProps.IsPersistent <- true
                do! ctx.SignInAsync(p, authProps) |> Async.AwaitTask

                Log.Information("google login done: {email} {name}", email, name)

                ctx.SetHttpHeader("Location", "/")
                return! setStatusCode 303 earlyReturn ctx
        }

let testSignIn (env: #_) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            Log.Information("test login start")
            let testUser = ctx.Request.Form.["test-user"][0]

            let config = env :> IConfiguration
            let email = testUser
            let userId = email |> UserClientId.TryCreate |> forceValidate

            let p = prepareClaimsPrincipal userId.Value config
            let authProps = AuthenticationProperties()
            authProps.IsPersistent <- true
            do! ctx.SignInAsync(p, authProps) |> Async.AwaitTask

            Log.Information("test login done: {email}", email)

            ctx.SetHttpHeader("Location", "/")
            return! setStatusCode 303 earlyReturn ctx
        }
