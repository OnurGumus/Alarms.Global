module HolidayTracker.MVU.SignIn

open HolidayTracker.Shared.Model
open Authentication

type Status =
    | NotLoggedIn
    | LoggedIn of UserClientId
    | AskEmail
    | AskVerification

type Model =
    { Status: Status
      UserClientId: UserClientId option
      IsBusy: bool }

type Msg =
    | LoginRequested // Ask for email
    | LoginCancelled // cancel login
    | EmailSubmitted of UserClientId //email entered
    | VerificationSubmitted of VerificationCode //verification code entered
    | EmailSent
    | EmailFailed of string
    | VerificationSuccessful
    | VerificationFailed
    | LogoutRequested
    | LogoutSuccess
    | LogoutError of string

type SideEffect =
    | NoEffect
    | Login of UserClientId
    | Verify of UserClientId * VerificationCode
    | Logout of UserClientId
    | ShowError of string
    | PublishLogin of UserClientId

let init (userName: string option) () =
    match userName with
    | Some name ->
        let userClientId = name |> UserClientId.TryCreate |> forceValidate |> Some

        { Status = LoggedIn(userClientId.Value)
          UserClientId = userClientId
          IsBusy = false },
        (PublishLogin(userClientId.Value))

    | None ->
        { Status = NotLoggedIn
          UserClientId = None
          IsBusy = false },
        NoEffect

let update msg model =
    match msg with
    | LoginRequested -> { model with Status = Status.AskEmail }, NoEffect
    | LoginCancelled ->
        { model with
            Status = Status.NotLoggedIn },
        NoEffect
    | EmailSubmitted email -> { model with UserClientId = Some email }, Login email
    | EmailSent ->
        { model with
            Status = Status.AskVerification },
        NoEffect
    | VerificationSubmitted code -> model, Verify(model.UserClientId.Value, code)
    | EmailFailed ex -> model, ShowError ex
    | VerificationSuccessful ->
        { model with
            Status = Status.LoggedIn model.UserClientId.Value },
        NoEffect
    | VerificationFailed -> model, ShowError "Verification failed"
    | LogoutSuccess ->
        { model with
            Status = Status.NotLoggedIn },
        NoEffect
    | LogoutError ex ->
        { model with
            Status = Status.NotLoggedIn },
        ShowError ex
    | LogoutRequested -> model, Logout model.UserClientId.Value
