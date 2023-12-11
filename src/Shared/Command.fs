module HolidayTracker.Shared.Command
open HolidayTracker.Shared.Model.Subscription

module Subscription =
    open Model

    type Subscribe = RegionId -> Async<Result<Version, string list>>
    type Unsubscribe = RegionId -> Async<Result<Version, string list>>
