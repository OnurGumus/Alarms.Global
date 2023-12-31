[<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
module Command.Actor

open System.Collections.Immutable
open Akka.Streams
open Akka.Persistence.Journal
open Akka.Actor
open Akka.Cluster
open Akka.Cluster.Tools.PublishSubscribe
open Akkling
open Microsoft.Extensions.Configuration
open Common.DynamicConfig
open System.Dynamic
open Command
open Akkling.Persistence
open Akka
open Common
open Akka.Logger.Serilog
open Akka.Event

let common<'TEvent, 'TState>
    (mailbox: Eventsourced<obj>)
    (publishEvent: Event<'TEvent> -> unit)
    (set: 'TState -> _)
    (state: 'TState)
    (applyNewState: Event<'TEvent> -> 'TState -> 'TState)
    body
    (msg: obj)
    =
    actor {
        let log = mailbox.UntypedContext.GetLogger()
        log.Debug("Message {MSG}, State: {@State}", box msg, state)

        match msg with
        | PersistentLifecycleEvent _
        | :? Persistence.SaveSnapshotSuccess
        | LifecycleEvent _ -> return! state |> set

        | SnapshotOffer(snapState: obj) -> return! snapState |> unbox<_> |> set

        // actor level events will come here
        | Persisted mailbox (:? Common.Event<'TEvent> as event) ->
            let version = event.Version
            publishEvent event

            let state = applyNewState event state

            if (version >= 30L && version % 30L = 0L) then
                return! state |> set <@> SaveSnapshot(state)
            else
                return! state |> set
        | _ -> return! (body msg)
    }

let private defaultTag = ImmutableHashSet.Create("default")

type Tagger =
    interface IWriteEventAdapter with
        member _.Manifest _ = ""
        member _.ToJournal evt = evt //box <| Tagged(evt, defaultTag)

    public new() = { }


type MyEventAdapter =
    interface IEventAdapter with
        member this.FromJournal(evt: obj, manifest: string) : IEventSequence = EventSequence.Single(evt)
        member this.Manifest(evt: obj) : string = ""
        member this.ToJournal(evt: obj) : obj = box <| Tagged(evt, defaultTag)

    public new() = { }

[<Interface>]
type IActor =
    abstract Mediator: Akka.Actor.IActorRef
    abstract Materializer: ActorMaterializer
    abstract System: ActorSystem
    abstract SubscribeForCommand: Common.CommandHandler.Command<'a, 'b> -> Async<Common.Event<'b>>
    abstract Stop: unit -> System.Threading.Tasks.Task

let api (config: IConfiguration) =
    let (akkaConfig: ExpandoObject) =
        unbox<_> (config.GetSectionAsDynamic("config:akka"))

    let config = Akka.Configuration.ConfigurationFactory.FromObject akkaConfig

    let system = System.create "cluster-system" config

    // SqlitePersistence.Get(system) |> ignore

    Cluster.Get(system).SelfAddress |> Cluster.Get(system).Join

    let mediator = DistributedPubSub.Get(system).Mediator

    let mat = ActorMaterializer.Create(system)

    let subscribeForCommand command =
        Common.CommandHandler.subscribeForCommand system (typed mediator) command

    { new IActor with
        member _.Mediator = mediator
        member _.Materializer = mat
        member _.System = system
        member _.SubscribeForCommand command = subscribeForCommand command
        member _.Stop() = system.Terminate() }
