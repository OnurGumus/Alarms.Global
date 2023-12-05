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
let google: obj = jsNative

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
                    dialog.removeAttribute ("hidden")
                    let googleSignin = document.getElementById ("google-signin")
                    google?accounts?id?renderButton (googleSignin, {| 
                        ``type``= "standard";
                         shape = "rectangular"; 
                         theme= "outline";
                         text= "signin_with";size= "medium"; 
                         logo_alignment="right";|})
                   
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
                <span class=username>{user.Value}</span>
                <form action="/sign-out" method="post">
                    <button type="submit">Sign Out</button>
                </form>
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
            <dialog aria-labelledby="signin-header" hidden {Lit.refValue dialogRef}> 
                <form method="dialog" >
                    <header> 
                        <h2 id="signin-header">Sign in</h2>
                        <button  type="button" @click={Ev(fun _ -> dispatch LoginCancelled)} title="Close dialog"> 
                            <title>Close dialog icon</title>
                            <svg width="24" height="24" viewBox="0 0 24 24">
                            <line x1="18" y1="6" x2="6" y2="18"/>
                            <line x1="6" y1="6" x2="18" y2="18"/>
                            </svg>
                        </button>
                    </header>

                    <article>
                        <div id="google-signin"></div>
                    </article>
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
