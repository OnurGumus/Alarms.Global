module HolidayTracker.Command.Domain.User

open Command
open Akkling
open Akkling.Persistence
open AkklingHelpers
open Akka
open Common
open Serilog
open System
open Akka.Cluster.Tools.PublishSubscribe
open Actor
open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Model.Authentication
open HolidayTracker.Shared.Model
open Akka.Logger.Serilog
open Akka.Event
open HolidayTracker.ServerInterfaces.Command
open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared


type Command =
    | Subscribe of UserSubscription
    | Unsubscribe of UserSubscription

type Event =
    | Subscribed of UserSubscription
    | RemindBeforeDaysSet of int
    | AlreadySubscribed of UserSubscription
    | Unsubscribed of UserSubscription
    | AlreadyUnsubscribed of UserSubscription

type State =
    { Subscriptions: UserSubscription Set
      RemindBeforeDays: int option
      Version: int64 }

    interface IDefaultTag


let actorProp (env: _) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let config = env :> IConfiguration
    let defaultReminderDays = config[Constants.DefaultReminderDays] |> int
    let log = mailbox.UntypedContext.GetLogger()

    let mediatorS = retype mediator
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self

    let rec set (state: State) =

        let apply (event: Event) (state: State) =
            log.Debug("Apply Message {@Event}, State: @{State}", event, state)

            match event with
            | Subscribed subs ->
                { state with
                    Subscriptions = state.Subscriptions.Add subs }
            | Unsubscribed subs ->
                { state with
                    Subscriptions = state.Subscriptions.Remove subs }
            | RemindBeforeDaysSet days ->
                { state with
                    RemindBeforeDays = Some days }
            | _ -> state

        actor {
            let! msg = mailbox.Receive()
            log.Debug("Message {MSG}, State: {@State}", box msg, state)

            match msg with
            | PersistentLifecycleEvent _
            | :? Persistence.SaveSnapshotSuccess
            | LifecycleEvent _ -> return! state |> set

            | SnapshotOffer(snapState: obj) -> return! snapState |> unbox<_> |> set

            // actor level events will come here
            | Persisted mailbox (:? Common.Event<Event> as event) ->
                let version = event.Version
                SagaStarter.publishEvent mailbox mediator event event.CorrelationId

                let state =
                    { (apply event.EventDetails state) with
                        Version = version }

                if (version >= 30L && version % 30L = 0L) then
                    return! state |> set <@> SaveSnapshot(state)
                else
                    return! state |> set

            | Recovering mailbox (:? Common.Event<Event> as event) ->
                return!
                    { (apply event.EventDetails state) with
                        Version = event.Version }
                    |> set

            | _ ->
                match msg with
                | :? Persistence.RecoveryCompleted ->
                    if state.RemindBeforeDays.IsNone then
                        let event = RemindBeforeDaysSet defaultReminderDays
                        let e = toEvent "" state.Version event |> box |> Persist
                        return! (state |> set) <@> e
                    else
                        return! state |> set

                | :? (Common.Command<Command>) as userMsg ->
                    let ci = userMsg.CorrelationId
                    let commandDetails = userMsg.CommandDetails
                    let v = state.Version

                    match commandDetails with
                    | Subscribe subs ->
                        let subscribeEvent, v =
                            if state.Subscriptions |> Set.contains subs then
                                AlreadySubscribed subs, v
                            else
                                Subscribed subs, (v + 1L)

                        let outcome = toEvent ci v subscribeEvent |> sendToSagaStarter ci |> box |> Persist

                        return! outcome
                    | Unsubscribe subs ->
                        let subscribeEvent, v =
                            if state.Subscriptions |> Set.contains subs |> not then
                                AlreadyUnsubscribed subs, v
                            else
                                Unsubscribed subs, (v + 1L)

                        let outcome = toEvent ci v subscribeEvent |> sendToSagaStarter ci |> box |> Persist
                        return! outcome

                | _ ->
                    log.Debug("Unhandled Message {@MSG}", box msg)
                    return Unhandled
        }

    set
        { Version = 0L
          RemindBeforeDays = None
          Subscriptions = Set.empty }

let init (env: _) toEvent (actorApi: IActor) =
    AkklingHelpers.entityFactoryFor actorApi.System shardResolver "User"
    <| propsPersist (actorProp env toEvent (typed actorApi.Mediator))
    <| false

let factory (env: _) toEvent actorApi entityId =
    (init env toEvent actorApi).RefFor DEFAULT_SHARD entityId
