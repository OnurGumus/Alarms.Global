module HolidayTracker.MVU.CountriesSelector

type Model = { IsLoggedIn: bool }

type Msg = SetLoggedIn of bool

type SideEffect = | NoEffect

let init (isLoggedIn: bool) () = { IsLoggedIn = isLoggedIn }, NoEffect

let update msg model =
    match msg with
    | SetLoggedIn loginStatus -> { IsLoggedIn = loginStatus }, NoEffect
