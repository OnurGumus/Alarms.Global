module ElmishSideEffect
open Elmish
open Lit.Elmish

let effect (dispatching:ElmishStore.Dispatch<'msg> ->unit) : ElmishStore.Cmd<'value, 'msg> =
    [fun _update dispatch -> dispatching dispatch]
module Program =
    /// <summary>
    /// Program with user-defined side ffects instead of usual command.
    /// Side effects are processed by <code>execute</code> which can dispatch messages,
    /// called in place of usual command processing.
    /// </summary>
    let mkHiddenProgramWithSideEffectExecute
            (init: 'arg' -> 'model * 'sideEffect)
            (update: 'msg -> 'model -> 'model * 'sideEffect)
            (execute: 'sideEffect -> Dispatch<'msg> -> unit) =
        let convert (model, sideEffect) = 
            model, sideEffect |> execute |> Cmd.ofEffect 
        Program.mkHidden
            (init >> convert)
            (fun msg model -> update msg model |> convert)
            
    let mkStoreWithSideEffectExecute
            (init: 'arg' -> 'model * 'sideEffect)
            (update: 'msg -> 'model -> 'model * 'sideEffect)
            (dispose: 'model -> unit)
            (execute: 'sideEffect ->ElmishStore.Dispatch<'msg> ->unit) =
        let convert (model, sideEffect) = 
            model, sideEffect |> execute |>effect
        Store.makeElmish
            (init >> convert)
            (fun msg model -> update msg model |> convert)
            dispose