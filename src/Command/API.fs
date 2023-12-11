module HolidayTracker.Command.API

open Command
open Common
open Serilog
open Actor
open NodaTime
open System
open Microsoft.Extensions.Configuration
open HolidayTracker.Shared.Command
open HolidayTracker.Shared.Command.Subscription
open HolidayTracker.Shared.Model
open HolidayTracker.Command.Domain.API

let createCommandSubscription (domainApi: IDomain) factory (id: string) command filter =
    let corID = id |> Uri.EscapeDataString |> SagaStarter.toNewCid
    let actor = factory id

    let commonCommand: Command<_> =
        { CommandDetails = command
          CreationDate = domainApi.Clock.GetCurrentInstant()
          CorrelationId = corID }

    let e =
        { Cmd = commonCommand
          EntityRef = actor
          Filter = filter }

    let ex = Execute e
    ex |> domainApi.ActorApi.SubscribeForCommand


[<Interface>]
type IAPI =
    abstract ActorApi: IActor
    abstract Subscribe: Subscribe
    abstract Unsubscribe: Unsubscribe

let api (env: _) (clock: IClock) =
    let config = env :> IConfiguration
    let actorApi = Actor.api config
    let domainApi = Domain.API.api env clock actorApi

    { new IAPI with
        member this.Subscribe: Subscribe = failwith "Not implemented"
        member this.Unsubscribe: Unsubscribe = failwith "Not implemented"
        member _.ActorApi = actorApi }
