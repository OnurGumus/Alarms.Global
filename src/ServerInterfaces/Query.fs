module HolidayTracker.ServerInterfaces.Query

open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared.Model
open Akka.Streams
open HolidayTracker.Shared.Model.Authentication
open Command

type SubscriptionEvent =
    | Subscribed of UserSubscription
    | Unsubscribed of UserSubscription

type IdentificationEvent = IdentificationSucceded of User

type UserSettingEvent = RemindBeforeDaysSet of UserIdentity * int

type GlobalEventEvent = Published of GlobalEvent

type DataEventType =
    | SubscriptionEvent of SubscriptionEvent
    | IdentificationEvent of IdentificationEvent
    | UserSettingEvent of UserSettingEvent
    | GlobalEventEvent of GlobalEventEvent

type DataEvent = { Type: DataEventType; CID: CID }

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
    abstract Subscribe: (DataEvent -> bool) * int * (DataEvent -> unit) -> IKillSwitch * Async<unit>
