module HolidayTracker.ServerInterfaces.Command

open HolidayTracker.Shared.Command.Subscription
open HolidayTracker.Shared.Command.Authentication
open HolidayTracker.Shared.Model
open System

type CID =
    | CID of ShortString

    member this.Value: string = let (CID v) = this in v.Value

    static member CreateNew() =
        Guid.NewGuid().ToString() |> ShortString.TryCreate |> forceValidate |> CID

    static member Create(s: string) =
        let s = if (s.Contains "~") then s.Split("~")[1] else s
        s |> ShortString.TryCreate |> forceValidate |> CID

[<Interface>]
type ISubscription =
    abstract Subscribe: CID -> Subscribe
    abstract Unsubscribe: CID -> Unsubscribe
    abstract PublishEvent: CID -> PublishEvent

[<Interface>]
type IAuthentication =
    abstract IdentifyUser: CID -> IdentifyUser
