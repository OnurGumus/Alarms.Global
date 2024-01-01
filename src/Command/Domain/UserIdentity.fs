module HolidayTracker.Command.Domain.UserIdentity

open Command
open Akkling
open Akkling.Persistence
open Akka
open Common
open Akka.Cluster.Tools.PublishSubscribe
open Actor
open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Model.Authentication
open HolidayTracker.Shared.Model
open Akka.Logger.Serilog
open Akka.Event

type Command = Identify of UserClientId

type Event =
    | IdentificationSucceded of User
    | AlreadyIdentified of User

type State =
    { User: User option
      Version: int64 }

    interface ISerializable
let actorProp (env: _) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let config = env :> IConfiguration
    let log = mailbox.UntypedContext.GetLogger()
    let mediatorS = retype mediator
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self

    let apply (event: Event<_>) (state: State) =
        log.Debug("Apply Message {@Event}, State: @{State}", event, state)

        match event.EventDetails with
        | IdentificationSucceded user -> { state with User = Some user }
        | _ -> state
        |> fun state -> { state with Version = event.Version }

    let rec set (state: State) =
        let body (msg: obj) =
            actor {
                match msg with
                | :? Persistence.RecoveryCompleted -> return! state |> set

                | :? (Common.Command<Command>) as userMsg ->
                    let ci = userMsg.CorrelationId
                    let commandDetails = userMsg.CommandDetails
                    let v = state.Version

                    match commandDetails with
                    | Identify userClientId ->
                        let identityEvent, v =
                            match state.User with
                            | Some user -> AlreadyIdentified user, v
                            | _ ->
                                let newIdentity = UserIdentity.CreateNew()

                                IdentificationSucceded
                                    { ClientId = userClientId
                                      Identity = newIdentity
                                      Version = Version(v + 1L) },
                                (v + 1L)

                        let identificationOutcome =
                            toEvent ci v identityEvent |> sendToSagaStarter ci |> box |> Persist

                        return! identificationOutcome
                | _ ->
                    log.Debug("Unhandled Message {@MSG}", box msg)
                    return Unhandled
            }
        common mailbox mediator set state apply body
    set { Version = 0L; User = None }

let init (env: _) toEvent (actorApi: IActor) =
    AkklingHelpers.entityFactoryFor actorApi.System shardResolver "UserIdentity"
    <| propsPersist (actorProp env toEvent (typed actorApi.Mediator))
    <| false

let factory (env: _) toEvent actorApi entityId =
    (init env toEvent actorApi).RefFor DEFAULT_SHARD entityId
