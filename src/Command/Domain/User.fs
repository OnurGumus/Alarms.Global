module HolidayTracker.Command.Domain.User

open Command
open Akkling
open Akkling.Persistence
open Akka
open Common
open System
open Akka.Cluster.Tools.PublishSubscribe
open Actor
open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Model.Authentication
open Akka.Logger.Serilog
open Akka.Event
open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared

type Command =
    | Subscribe of UserSubscription
    | Unsubscribe of UserSubscription

type Event =
    | Subscribed of UserSubscription
    | RemindBeforeDaysSet of UserIdentity * int
    | AlreadySubscribed of UserSubscription
    | Unsubscribed of UserSubscription
    | AlreadyUnsubscribed of UserSubscription

type State =
    { Subscriptions: UserSubscription Set
      RemindBeforeDays: int option
      Version: int64 }

    interface ISerializable

let actorProp (env: _) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let config = env :> IConfiguration
    let defaultReminderDays = config[Constants.DefaultReminderDays] |> int
    let log = mailbox.UntypedContext.GetLogger()

    let mediatorS = retype mediator
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self

    let apply (event: Event<_>) (state: State) =
        log.Debug("Apply Message {@Event}, State: @{State}", event, state)

        match event.EventDetails with
        | Subscribed subs ->
            { state with
                Subscriptions = state.Subscriptions.Add subs }
        | Unsubscribed subs ->
            { state with
                Subscriptions = state.Subscriptions.Remove subs }
        | RemindBeforeDaysSet(_, days) ->
            { state with
                RemindBeforeDays = Some days }
        | _ -> state
        |> fun state -> { state with Version = event.Version }

    let rec set (state: State) =

        let body (msg: obj) =
            actor {
                let v = state.Version

                match msg with
                | :? Persistence.RecoveryCompleted -> return! state |> set

                | :? (Common.Command<Command>) as userMsg ->
                    let ci = userMsg.CorrelationId
                    let commandDetails = userMsg.CommandDetails

                    match commandDetails with
                    | Subscribe subs ->

                        let subscribeEvent, v =
                            if state.Subscriptions |> Set.contains subs then
                                AlreadySubscribed subs, v
                            else
                                Subscribed subs, (v + 1L)

                        let outcome = toEvent ci v subscribeEvent |> sendToSagaStarter ci |> box |> Persist

                        if state.RemindBeforeDays.IsNone then
                            let event = RemindBeforeDaysSet(subs.Identity, defaultReminderDays)
                            let e = toEvent (Guid.NewGuid().ToString()) state.Version event |> box |> Persist
                            return! outcome <@> e
                        else
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

        common mailbox mediator set state apply body
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
