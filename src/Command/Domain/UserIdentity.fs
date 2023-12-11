module HolidayTracker.Command.Domain.UserIdentity

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


type Command =
    | Identify

type Event =
    | IdentificationSucceded of UserIdentity
    | AlreadyIdentified of UserIdentity

type State = {
    Identity: UserIdentity option
    Version: int64
} with
    interface IDefaultTag


let actorProp (env:_) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let config  = env :> IConfiguration
    let log = mailbox.UntypedContext.GetLogger()
    let mediatorS = retype mediator
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self
    let rec set (state: State) =

        let apply (event: Event) (state:State) =
            log.Debug("Apply Message {@Event}, State: @{State}", event, state)
            match event with
            | IdentificationSucceded identity->
                {
                    state with
                        Identity = Some identity
                }
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

                let state = {
                    (apply event.EventDetails state) with
                        Version = version
                }

                if (version >= 30L && version % 30L = 0L) then
                    return! state |> set <@> SaveSnapshot(state)
                else
                    return! state |> set
                    
            | Recovering mailbox (:? Common.Event<Event> as event) ->
                return!
                    {
                        (apply event.EventDetails state) with
                            Version = event.Version
                    }
                    |> set

            | _ ->
                match msg with
                | :? Persistence.RecoveryCompleted -> return! state |> set

                | :? (Common.Command<Command>) as userMsg ->
                    let ci = userMsg.CorrelationId
                    let commandDetails = userMsg.CommandDetails
                    let v = state.Version

                    match commandDetails with
                    | Identify ->
                        let identityEvent,v =
                            match state.Identity with
                            | Some identity -> AlreadyIdentified identity, v
                            | _ ->
                                let newIdentity = UserIdentity.CreateNew()
                                IdentificationSucceded newIdentity, (v + 1L)

                        let identificationOutcome =
                            toEvent ci v identityEvent |> sendToSagaStarter ci |> box |> Persist
                        return! identificationOutcome

                | _ ->
                        log.Debug("Unhandled Message {@MSG}", box msg)
                        return Unhandled
        }
    set {
        Version = 0L
        Identity = None
    }

let init (env: _) toEvent (actorApi: IActor) =
    AkklingHelpers.entityFactoryFor actorApi.System shardResolver "UserIdentity"
    <| propsPersist (actorProp env toEvent (typed actorApi.Mediator))
    <| false

let factory (env: _) toEvent actorApi entityId =
    (init env toEvent actorApi).RefFor DEFAULT_SHARD entityId