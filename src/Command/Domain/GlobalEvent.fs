module HolidayTracker.Command.Domain.GlobalEvent

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

type Command = Publish of GlobalEvent

type Event = Published of GlobalEvent

type State =
    { Version: int64 }

    interface IDefaultTag

let actorProp (env: _) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let log = mailbox.UntypedContext.GetLogger()

    let mediatorS = retype mediator
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self

    let rec set (state: State) =

        let apply (event: Event) (state: State) =
            log.Debug("Apply Message {@Event}, State: @{State}", event, state)

            match event with
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
                | :? Persistence.RecoveryCompleted -> return! state |> set

                | :? (Common.Command<Command>) as userMsg ->
                    let ci = userMsg.CorrelationId
                    let commandDetails = userMsg.CommandDetails
                    let v = state.Version

                    match commandDetails with
                    | Publish globalEvent ->

                        let e = Published globalEvent

                        let outcome = toEvent ci (v + 1L) e |> sendToSagaStarter ci |> box |> Persist
                        return! outcome

                | _ ->
                    log.Debug("Unhandled Message {@MSG}", box msg)
                    return Unhandled
        }

    set { Version = 0L }

let init (env: _) toEvent (actorApi: IActor) =
    AkklingHelpers.entityFactoryFor actorApi.System shardResolver "GlobalEvent"
    <| propsPersist (actorProp env toEvent (typed actorApi.Mediator))
    <| false

let factory (env: _) toEvent actorApi entityId =
    (init env toEvent actorApi).RefFor DEFAULT_SHARD entityId
