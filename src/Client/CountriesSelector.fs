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

let private hmr = HMR.createToken ()

[<LitElement("ht-countries-selector")>]
let LitElement () =
    Hook.useHmr (hmr)

    let (host: HTMLElement), _ =
        !! LitElement.init (fun (config: LitConfig<_>) -> config.useShadowDom <- false)

    Hook.useEffectOnce (fun () ->
        let onClick (e: Event) =
            e.preventDefault ()
            window.alert (e.target?getAttribute ("data-name"))

        let countrySelectors: seq<HTMLElement> =
            !! host.querySelectorAll(".country-selector")

        countrySelectors
        |> Seq.iter (fun (s: HTMLElement) -> s.addEventListener ("click", onClick))

        Hook.createDisposable (fun () ->
            countrySelectors
            |> Seq.iter (fun (s: HTMLElement) -> s.removeEventListener ("click", onClick))))

let register () = ()
