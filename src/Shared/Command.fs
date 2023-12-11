module HolidayTracker.Shared.Command
open HolidayTracker.Shared.Model
open HolidayTracker.Shared.Model.Subscription
open HolidayTracker.Shared.Model.Authentication

module Authentication = 
    type AssignUserId = UserClientId -> Async<Result<Version, string list>>

module Subscription =

    type Subscribe = RegionId -> Async<Result<Version, string list>>
    type Unsubscribe = RegionId -> Async<Result<Version, string list>>
