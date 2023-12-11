module HolidayTracker.ServerInterfaces.Query
open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared.Model
open Akka.Streams

type SubscriptionEvent = 
    | Subscribed of UserSubscription
    | Unsubscribed of UserSubscription

type DataEvent = SubscriptionEvent of SubscriptionEvent

[<Interface>]
type IQuery =
    abstract Query<'t> : ?filter:Predicate * ?orderby:string * ?orderbydesc:string * ?thenby:string  * ?thenbydesc:string * ?take:int * ?skip:int -> list<'t> Async
    abstract Subscribe:(DataEvent -> unit)->IKillSwitch