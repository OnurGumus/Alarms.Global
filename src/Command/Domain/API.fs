module HolidayTracker.Command.Domain.API

open Akkling
open Akkling.Persistence
open AkklingHelpers
open Akka
open Command
open Common
open Akka.Cluster.Sharding
open Serilog
open System
open Akka.Cluster.Tools.PublishSubscribe
open NodaTime
open Actor
open Akkling.Cluster.Sharding
open Microsoft.Extensions.Configuration

let sagaCheck (env: _) toEvent actorApi (clock: IClock) (o: obj) =
    match o with
    | _ -> []


[<Interface>]
type IDomain =
    abstract ActorApi: IActor
    abstract Clock: IClock
    abstract UserIdentityFactory: string -> IEntityRef<obj>

let api (env: _) (clock: IClock) (actorApi: IActor) =
    let toEvent ci = Common.toEvent clock ci
    SagaStarter.init actorApi.System actorApi.Mediator (sagaCheck env toEvent actorApi clock)

    UserIdentity.init env toEvent actorApi
    |> sprintf "UserIdentity initialized: %A"
    |> Log.Debug

    System.Threading.Thread.Sleep(1000)

    { new IDomain with
        member _.Clock = clock
        member _.ActorApi = actorApi

        member _.UserIdentityFactory entityId =
            UserIdentity.factory env toEvent actorApi entityId }
