module HolidayTracker.Shared.Constants

[<Literal>]
let ConfigHocon = "config.hocon"

[<Literal>]
let UserIdentity = "UserIdentity"

[<Literal>]
let ConnectionString = "config:connection-string"
module Events =
    [<Literal>]
    let LOGGED_IN = "loggedIn"

    [<Literal>]
    let LOGGED_OUT = "loggedOut"

    [<Literal>]
    let ERROR_OCCURRED = "errorOccured"

    [<Literal>]
    let LOGIN_REQUESTED = "loginRequested"
