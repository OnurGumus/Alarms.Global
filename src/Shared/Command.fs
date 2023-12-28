module HolidayTracker.Shared.Command

open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared.Model.Authentication

module Authentication =
    type IdentifyUser = UserClientId -> Async<Result<Version, string list>>

module Subscription =

    type Subscribe = UserIdentity option -> RegionId -> Async<Result<Version, string list>>
    type Unsubscribe = UserIdentity option -> RegionId -> Async<Result<Version, string list>>
    type PublishEvent = GlobalEvent -> Async<Result<Version, string list>>
