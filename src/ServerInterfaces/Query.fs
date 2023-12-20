module HolidayTracker.ServerInterfaces.Query

open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared.Model
open Akka.Streams
open HolidayTracker.Shared.Model.Authentication

type SubscriptionEvent =
    | Subscribed of UserSubscription
    | Unsubscribed of UserSubscription

type IdentificationEvent = IdentificationSucceded of User

type UserSettingEvent = RemindBeforeDaysSet of UserIdentity * int

type DataEvent =
    | SubscriptionEvent of SubscriptionEvent
    | IdentificationEvent of IdentificationEvent
    | UserSettingEvent of UserSettingEvent

[<Interface>]
type IQuery =
    abstract Query<'t> :
        ?filter: Predicate *
        ?orderby: string *
        ?orderbydesc: string *
        ?thenby: string *
        ?thenbydesc: string *
        ?take: int *
        ?skip: int ->
            list<'t> Async

    abstract Subscribe: (DataEvent -> unit) -> IKillSwitch
