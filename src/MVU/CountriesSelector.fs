module HolidayTracker.MVU.CountriesSelector

type Model = { IsLoggedIn: bool }

type Msg = SetLoggedIn of bool

type SideEffect = NoEffect | SubscribeToLogin

let init (isLoggedIn: bool) () = { IsLoggedIn = isLoggedIn }, SubscribeToLogin

let update msg model =
    match msg with
    | SetLoggedIn loginStatus -> { IsLoggedIn = loginStatus }, NoEffect
