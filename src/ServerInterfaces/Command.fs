module HolidayTracker.ServerInterfaces.Command

open HolidayTracker.Shared.Command.Subscription
open HolidayTracker.Shared.Command.Authentication

[<Interface>]
type ISubscription =
    abstract Subscribe: Subscribe
    abstract Unsubscribe: Unsubscribe

[<Interface>]
type IAuthentication =
    abstract IdentifyUser: IdentifyUser
