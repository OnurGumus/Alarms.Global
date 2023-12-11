module HolidayTracker.Shared.Command

module Subscription =
    open Model

    type Subscribe = RegionId -> Async<Result<Version, string list>>
    type Unsubscribe = RegionId -> Async<Result<Version, string list>>
