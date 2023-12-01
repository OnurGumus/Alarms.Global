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
            <button type=button @click={Ev(fun _ -> dispatch LoginRequested)}> Sign In</button>
        """
    | AskVerification
    | AskEmail ->
        html
            $"""
            <dialog {Lit.refValue dialogRef}>
            <button type=button class="close deny" @click={Ev(fun _ -> dispatch LoginCancelled)}>
                <img src="/assets/icons/icons8-close.svg" alt='close button'>
            </button>
            <h2> Sign in or Sign up </h2>

             <div> dialog </div>
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
