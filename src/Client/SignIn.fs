module HolidayTracker.Client.SignIn

open Elmish
open Elmish.HMR
open Lit
open Lit.Elmish
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open System
open Browser
open Elmish.Debug
open FsToolkit.ErrorHandling
open ElmishSideEffect
open Browser.Types
open HolidayTracker.MVU
open SignIn
open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Constants
open Authentication

let private hmr = HMR.createToken ()

let rec execute (host: LitElement) order dispatch =
    match order with
    | PublishLogin email -> LoginStore.dispatcher <| LoginStore.Msg.LoggedIn email

    | ShowError ex -> window.alert ex

    | _ -> ()
[<Global>]
let google:obj = jsNative

[<HookComponent>]
let view (host: LitElement) (model: Model) dispatch =

    let dialogRef = Hook.useRef<HTMLDialogElement> ()

    Hook.useEffectOnce (fun () ->
        let requestLogin _ = dispatch LoginRequested

        document.addEventListener (Events.LOGIN_REQUESTED, requestLogin) |> ignore

        Hook.createDisposable
        <| fun () -> document.removeEventListener (Events.LOGIN_REQUESTED, requestLogin))

    Hook.useEffectOnChange (
        model.Status,
        fun state ->
            let dialog = dialogRef.Value

            match state, dialog with
            | AskEmail, Some dialog ->
                if not (dialog.``open``) then
                    dialog.addEventListener (
                        "close",
                        Ev(fun _ ->
                            document.body.focus ()
                            dispatch LoginCancelled)
                    )

                    dialog.showModal ()
                    dialog.removeAttribute("hidden")
                    let googleSignin = document.getElementById("g_id_onload2")
                    googleSignin?style?width <- "208px"
                    google?accounts?id?renderButton( googleSignin,{|  |})
            | _ -> ()
    )

    let emailField =
        match model.Status with
        | AskEmail ->
            html
                $"""
            <div class="form-field">
                <label>Email:</label>
                <div>
                    <input name=email placeholder="Email" type=email required  />
                </div>
            </div>
            """
        | AskVerification ->
            html
                $"""
            <div class="form-field">
                <label>Verification:</label>
                <div>
                    <input name=verification placeholder="Verification" type=text required  />
                </div>
            </div>
            """
        | _ -> Lit.nothing

    match model.Status with
    | LoggedIn user ->
        html
            $"""
         <div class=user-info>
            <img src="/assets/user.svg" class="user-icon" />
            <div>
                <span class=username>{user.Value}</span>
                <a class=sign-out
                @click={Ev(fun _ -> dispatch (LogoutRequested))}>Sign Out</a>
            </div>
        </div>
        """

    | NotLoggedIn ->
        html
            $"""
            <button type=button @click={Ev(fun _ -> dispatch LoginRequested)}>Sign In</button>
        """
    | AskVerification
    | AskEmail ->
        html
            $"""
            <dialog hidden {Lit.refValue dialogRef}> 
                <form method="dialog" >
                    <header> 
                        <h2>Sign in</h2>
                        <button type="button" @click={Ev(fun _ -> dispatch LoginCancelled)} title="Close dialog"> 
                            <title>Close dialog icon</title>
                            <svg width="24" height="24" viewBox="0 0 24 24">
                            <line x1="18" y1="6" x2="6" y2="18"/>
                            <line x1="6" y1="6" x2="18" y2="18"/>
                            </svg>
                        </button>
                    </header>

                    <article>
                    <div>
                        <div id="g_id_onload2"
                                data-client_id="961379412830-oe2516pvftiv91i2hga07u4n96vtu1lr.apps.googleusercontent.com"
                                data-context="signin"
                                data-ux_mode="popup"
                                data-login_uri="http://localhost:5070/signin-google"
                                data-auto_prompt="false"></div>

                        <div class="g_id_signin"
                            data-type="standard"
                            data-shape="rectangular"
                            data-theme="outline"
                            data-text="signin_with"
                            data-size="small"
                            data-logo_alignment="left">
                        </div>
                    </div>
                        <p>Lorem ipsum dolor sit amet consectetur adipisicing elit. Consequuntur, nesciunt alias. Tenetur, eos reiciendis deserunt possimus sit minus earum aspernatur?</p>
                        <p>Lorem ipsum dolor sit amet consectetur adipisicing elit. Consequuntur, nesciunt alias. Tenetur, eos reiciendis deserunt possimus sit minus earum aspernatur?</p>
                    </article>
                    <footer>
                        <menu>
                            <button type="reset" @click={Ev(fun _ -> dispatch LoginCancelled)}>Cancel</button>
                            <button type="submit" value="confirm">Confirm</button>
                        </menu>
                    </footer>
                </form>
            </dialog>
        """

[<LitElement("ht-signin")>]
let LitElement () =
    Hook.useHmr (hmr)

    let host, prop =
        LitElement.init (fun config ->
            config.useShadowDom <- false
            config.props <- {| username = Prop.Of(Option.None, attribute = "username") |})

    let program =
        Program.mkHiddenProgramWithSideEffectExecute (init prop.username.Value) update (execute host)
#if DEBUG
        |> Program.withDebugger
        |> Program.withConsoleTrace
#endif
    let model, dispatch = Hook.useElmish program
    view host model dispatch

let register () = ()
