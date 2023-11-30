module HolidayTracker.MVU.LoginStore

open HolidayTracker.Shared.Model.Authentication

type Model = { UserClientId: UserClientId option }

type Msg =
    | LoggedIn of UserClientId
    | LoggedOut

[<RequireQualifiedAccessAttribute>]
type SideEffect = | NoEffect

let init () =
    { UserClientId = None }, SideEffect.NoEffect

let update (msg: Msg) (model: Model) =
    match msg with
    | LoggedIn userId ->
        { model with
            UserClientId = Some userId },
        SideEffect.NoEffect
    | LoggedOut -> { model with UserClientId = None }, SideEffect.NoEffect

let dispose _ = ()
