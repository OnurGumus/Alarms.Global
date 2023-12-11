module HolidayTracker.Shared.API

open Command.Subscription

type Subscription =
    { Subscribe: Subscribe
      Unsubscribe: Unsubscribe }

module Route =
    let builder queryString typeName methodName =
        match queryString with
        | None -> sprintf "/api/%s/%s" typeName methodName
        | Some queryString -> sprintf "/api/%s/%s?%s" typeName methodName queryString
