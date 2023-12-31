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
open Akka.Logger.Serilog
open Akka.Event
open HolidayTracker.Shared.Model.Subscription

type Command = Publish of GlobalEvent

type Event =
    | Published of GlobalEvent
    | EventAlreadyPublished of GlobalEvent

type State =
    { GlobalEvent: GlobalEvent option
      Version: int64 }

    interface IDefaultTag

let actorProp (env: _) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let log = mailbox.UntypedContext.GetLogger()

    let mediatorS = retype mediator
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self

    let publishEvent event =
        SagaStarter.publishEvent mailbox mediator event event.CorrelationId

    let rec set (state: State) =

        let apply (event: Event) (state: State) =
            log.Debug("Apply Message {@Event}, State: @{State}", event, state)

            match event with
            | Published globalEvent ->
                { state with
                    GlobalEvent = Some globalEvent }
            | _ -> state

        let applyNewState event state =
            { (apply event.EventDetails state) with
                Version = event.Version }

        let body (msg: obj) =
            actor {
                let v = state.Version
                match msg with
                | :? Persistence.RecoveryCompleted -> return! state |> set

                | :? (Common.Command<Command>) as userMsg ->
                    let ci = userMsg.CorrelationId

                    match userMsg.CommandDetails with
                    | Publish globalEvent ->
                        let e, v =
                            if state.GlobalEvent.IsNone then
                                Published globalEvent, (v + 1L)
                            else
                                EventAlreadyPublished globalEvent, v

                        return! toEvent ci v e |> sendToSagaStarter ci |> box |> Persist
                | _ ->
                    log.Debug("Unhandled Message {@MSG}", box msg)
                    return Unhandled
            }

        actor {
            let! msg = mailbox.Receive()
            return! common mailbox publishEvent set state applyNewState body msg
        }

    set { Version = 0L; GlobalEvent = None }

let init (env: _) toEvent (actorApi: IActor) =
    AkklingHelpers.entityFactoryFor actorApi.System shardResolver "GlobalEvent"
    <| propsPersist (actorProp env toEvent (typed actorApi.Mediator))
    <| false

let factory (env: _) toEvent actorApi entityId =
    (init env toEvent actorApi).RefFor DEFAULT_SHARD entityId
