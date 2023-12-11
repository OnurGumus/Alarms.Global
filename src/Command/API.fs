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
open HolidayTracker.Shared.Command.Authentication
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

module UserIdentity =
    open HolidayTracker.Shared.Model.Authentication

    let identifyUser (createSubs) : IdentifyUser =
        fun userId ->
            async {
                Log.Debug("Inside identifyUser {@userId}", userId)

                let subscribA =
                    createSubs (userId.Value) (Domain.UserIdentity.Identify) (function
                        | Domain.UserIdentity.AlreadyIdentified _
                        | Domain.UserIdentity.IdentificationSucceded _ -> true)

                let! subscrib = subscribA

                match subscrib with
                | { EventDetails = Domain.UserIdentity.IdentificationSucceded _
                    Version = v }
                | { EventDetails = Domain.UserIdentity.AlreadyIdentified _
                    Version = v } -> return Ok(Version v)
            }

[<Interface>]
type IAPI =
    abstract ActorApi: IActor
    abstract Subscribe: Subscribe
    abstract Unsubscribe: Unsubscribe
    abstract IdentifyUser: IdentifyUser

let api (env: _) (clock: IClock) =
    let config = env :> IConfiguration
    let actorApi = Actor.api config
    let domainApi = Domain.API.api env clock actorApi

    let userIdentitySubs =
        createCommandSubscription domainApi domainApi.UserIdentityFactory

    { new IAPI with
        member this.Subscribe: Subscribe = failwith "Not implemented"
        member this.Unsubscribe: Unsubscribe = failwith "Not implemented"
        member this.IdentifyUser: IdentifyUser = UserIdentity.identifyUser userIdentitySubs
        member _.ActorApi = actorApi }
