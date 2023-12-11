module HolidayTracker.ServerInterfaces.Command

open HolidayTracker.Shared.Command.Subscription

[<Interface>]
type ISubscription =
    abstract Subscribe: Subscribe
    abstract Unsubscribe: Unsubscribe
