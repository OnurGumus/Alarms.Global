module HolidayTracker.Client.CountriesSelector


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
open HolidayTracker.MVU
open Thoth.Json
open HolidayTracker.Shared
open CountriesSelector
open ElmishSideEffect

let private hmr = HMR.createToken ()

let rec execute (host: HTMLElement) sideEffect (dispatch: Msg -> unit) =
    match sideEffect with
    | NoEffect -> ()
    | SubscribeToLogin ->
        LoginStore.store.Subscribe(fun (model: LoginStore.Model) -> dispatch (SetLoggedIn <| model.UserClientId.IsSome))
        |> ignore

[<HookComponent>]
let view (host: HTMLElement) (model: Model) dispatch =
    Hook.useEffectOnce (fun () ->
        let onClick (e: Event) =
            if model.IsLoggedIn |> not then
                e.preventDefault ()

                (host :?> LitElement)
                    .dispatchCustomEvent (
                        Constants.Events.LOGIN_REQUESTED,
                        (e.target?getAttribute ("data-name")),
                        true,
                        true,
                        true
                    )

        let countrySelectors: seq<HTMLElement> =
            !! host.querySelectorAll(".country-selector")

        countrySelectors
        |> Seq.iter (fun (s: HTMLElement) -> s.addEventListener ("click", onClick))

        Hook.createDisposable (fun () ->
            countrySelectors
            |> Seq.iter (fun (s: HTMLElement) -> s.removeEventListener ("click", onClick))))

    Lit.nothing

[<LitElement("ht-countries-selector")>]
let LitElement () =
    Hook.useHmr (hmr)

    let (host: HTMLElement), _ =
        !! LitElement.init (fun (config: LitConfig<_>) -> config.useShadowDom <- false)

    let program =
        Program.mkHiddenProgramWithSideEffectExecute (init false) (update) (execute host)
        |> Program.withDebugger
        |> Program.withConsoleTrace

    let model, dispatch = Hook.useElmish program
    view host model dispatch

let register () = ()
