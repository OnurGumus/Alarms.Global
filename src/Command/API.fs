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

module User =
    open HolidayTracker.Shared.Model.Subscription

    let subscribe (createSubs) : Subscribe =
        fun (userIdentity) regionId ->
            async {
                Log.Debug("Inside subscribe {@userId}", userIdentity.Value.Value.Value)

                let subscription: UserSubscription =
                    { Identity = userIdentity.Value
                      RegionId = regionId }

                let! subscribe =
                    createSubs (userIdentity.Value.Value.Value) (Domain.User.Command.Subscribe subscription) (function
                        | Domain.User.Subscribed _
                        | Domain.User.AlreadySubscribed _ -> true
                        | _ -> false)

                match subscribe with
                | { EventDetails = Domain.User.Subscribed _
                    Version = v }
                | { EventDetails = Domain.User.AlreadySubscribed _
                    Version = v } -> return Ok(Version v)
                | other -> return failwithf "unexpected event %A" other
            }

    let unsubscribe (createSubs) : Subscribe =
        fun userIdentity regionId ->
            async {
                Log.Debug("Inside unsubscribe {@userId}", userIdentity.Value.Value)

                let subscription: UserSubscription =
                    { Identity = userIdentity.Value
                      RegionId = regionId }

                let! subscribe =
                    createSubs (userIdentity.Value.Value.Value) (Domain.User.Command.Unsubscribe subscription) (function
                        | Domain.User.Unsubscribed _
                        | Domain.User.AlreadyUnsubscribed _ -> true
                        | _ -> false)

                match subscribe with
                | { EventDetails = Domain.User.Unsubscribed _
                    Version = v }
                | { EventDetails = Domain.User.AlreadyUnsubscribed _
                    Version = v } -> return Ok(Version v)
                | other -> return failwithf "unexpected event %A" other
            }

module UserIdentity =
    open HolidayTracker.Shared.Model.Authentication

    let identifyUser (createSubs) : IdentifyUser =
        fun userId ->
            async {
                Log.Debug("Inside identifyUser {@userId}", userId)

                let! subscribe =
                    createSubs (userId.Value) (Domain.UserIdentity.Command.Identify userId) (function
                        | Domain.UserIdentity.AlreadyIdentified _
                        | Domain.UserIdentity.IdentificationSucceded _ -> true)

                match subscribe with
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

    let userSubs = createCommandSubscription domainApi domainApi.UserFactory

    { new IAPI with
        member this.Subscribe: Subscribe = User.subscribe userSubs
        member this.Unsubscribe: Unsubscribe = User.unsubscribe userSubs
        member this.IdentifyUser: IdentifyUser = UserIdentity.identifyUser userIdentitySubs
        member _.ActorApi = actorApi }
