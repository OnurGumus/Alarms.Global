module HolidayTracker.Query.Projection

open FSharp.Data.Sql
open Serilog
open FSharp.Data.Sql.Common
open Thoth.Json.Net
open HolidayTracker.Shared.Model.Authentication
open HolidayTracker
open HolidayTracker.Shared.Model
open HolidayTracker.ServerInterfaces.Query

let extraThoth = Extra.empty |> Extra.withInt64 |> Extra.withDecimal

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + @"/libs"

[<Literal>]
let schemaLocation = __SOURCE_DIRECTORY__ + @"/../Server/Database/Schema.sqlite"
#if DEBUG

[<Literal>]
let connectionString =
    @"Data Source="
    + __SOURCE_DIRECTORY__
    + @"/../Server/Database/HolidayTracker.db;"

#else

[<Literal>]
let connectionString = @"Data Source=" + @"Database/HolidayTracker.db;"

#endif


type Sql =
    SqlDataProvider<DatabaseProviderTypes.SQLITE, SQLiteLibrary=SQLiteLibrary.MicrosoftDataSqlite, ContextSchemaPath=schemaLocation, ConnectionString=connectionString, ResolutionPath=resolutionPath, CaseSensitivityChange=CaseSensitivityChange.ORIGINAL>

QueryEvents.SqlQueryEvent
|> Event.add (fun query -> Log.Debug("Executing SQL {query}:", query))


let inline encode<'T> =
    Encode.Auto.generateEncoderCached<'T> (caseStrategy = CamelCase, extra = extraThoth)
    >> Encode.toString 0

let inline decode<'T> =
    Decode.Auto.generateDecoderCached<'T> (caseStrategy = CamelCase, extra = extraThoth)
    |> Decode.fromString

open Command.Actor
open Akka.Persistence.Query
open Akka.Persistence.Query.Sql
open FSharp.Data.Sql.Common
open Akka.Streams
open Akkling.Streams
open System.Threading
open HolidayTracker.ServerInterfaces.Command

let handleEventWrapper (ctx: Sql.dataContext) (actorApi: IActor) (subQueue: ISourceQueue<_>) (envelop: EventEnvelope) =
    try
        Log.Debug("Envelop:{@envelop}", envelop)
        let offsetValue = (envelop.Offset :?> Sequence).Value

        let dataEvent =
            match envelop.Event with
            | :? Command.Common.Event<Command.Domain.GlobalEvent.Event> as { EventDetails = eventDetails
                                                                             CorrelationId = cid
                                                                             Version = v } ->
                match eventDetails with
                | Command.Domain.GlobalEvent.EventAlreadyPublished _ -> None
                | Command.Domain.GlobalEvent.Published globalEvent ->
                    let cid = cid |> CID.Create


                    let globalEventRow =
                        ctx.Main.GlobalEvents.``Create(Body, Title)`` (globalEvent.Body.Value, globalEvent.Title.Value)

                    globalEventRow.Id <- globalEvent.GlobalEventId.Value.Value

                    globalEventRow.TargetDate <-
                        globalEvent.EventDateInUTC
                        |> function
                            | Some d -> d.ToString("yyyy-MM-dd HH:mm:ss")
                            | _ -> ""

                    for region in globalEvent.TargetRegion do
                        let regionId = ctx.Main.EventsRegions.Create()
                        regionId.EventId <- globalEventRow.Id
                        regionId.RegionId <- region.Value.Value

                    Some(
                        { Type = GlobalEventEvent(Published(globalEvent))
                          CID = cid }
                    )
            | :? Command.Common.Event<Command.Domain.UserIdentity.Event> as { EventDetails = eventDetails
                                                                              CorrelationId = cid
                                                                              Version = v } ->
                match eventDetails with
                | Command.Domain.UserIdentity.AlreadyIdentified _ -> None
                | Command.Domain.UserIdentity.IdentificationSucceded user ->
                    let row =
                        ctx.Main.UserIdentities.``Create(ClientId, Version)`` (user.ClientId.Value, user.Version.Value)

                    row.Identity <- user.Identity.Value.Value

                    Some(
                        { Type = IdentificationEvent(IdentificationSucceded user)
                          CID = cid |> CID.Create }
                    )

            | :? Command.Common.Event<Command.Domain.User.Event> as { EventDetails = eventDetails
                                                                      CorrelationId = cid
                                                                      Version = v } ->
                let cid = cid |> CID.Create

                match eventDetails with
                | Command.Domain.User.RemindBeforeDaysSet(identity, days) ->
                    let row = ctx.Main.UserSettings.``Create(ReminderDays, Version)`` (days, v)
                    row.Identity <- identity.Value.Value

                    Some(
                        { Type = UserSettingEvent(RemindBeforeDaysSet(identity, days))
                          CID = cid }
                    )

                | Command.Domain.User.AlreadySubscribed _ -> None
                | Command.Domain.User.Subscribed subs ->
                    let row = ctx.Main.Subscriptions.Create()
                    row.Identity <- subs.Identity.Value.Value
                    row.RegionId <- subs.RegionId.Value.Value

                    Some
                        { Type = SubscriptionEvent(Subscribed subs)
                          CID = cid }

                | Command.Domain.User.AlreadyUnsubscribed _ -> None
                | Command.Domain.User.Unsubscribed subs ->
                    let row =
                        query {
                            for c in (ctx.Main.Subscriptions) do
                                where (c.Identity = subs.Identity.Value.Value && c.RegionId = subs.RegionId.Value.Value)
                                take 1
                                select c
                        }
                        |> Seq.head

                    row.Delete()

                    Some
                        { Type = SubscriptionEvent(Unsubscribed subs)
                          CID = cid }

            | _ -> None

        let offset = ctx.Main.Offsets.Individuals.HolidayTracker
        offset.OffsetCount <- offsetValue

        ctx.SubmitUpdates()

        match (dataEvent: DataEvent option) with
        | Some dataEvent -> subQueue.OfferAsync(dataEvent).Wait()
        | _ -> ()
    with ex ->
        Log.Fatal(ex, "Error during event handling")
        actorApi.System.Terminate().Wait()
        Log.CloseAndFlush()
        System.Environment.Exit(-1)

let readJournal system =
    PersistenceQuery
        .Get(system)
        .ReadJournalFor<Akka.Persistence.Sql.Query.SqlReadJournal>(SqlReadJournal.Identifier)

let init (connectionString: string) (actorApi: IActor) =
    Log.Information("init query side")
    let ctx = Sql.GetDataContext(connectionString)

    let conn = ctx.CreateConnection()
    conn.Open()
    let cmd = conn.CreateCommand()
    cmd.CommandText <- "PRAGMA journal_mode=WAL;"
    cmd.ExecuteNonQuery() |> ignore
    conn.Dispose() |> ignore

    let offsetCount = ctx.Main.Offsets.Individuals.HolidayTracker.OffsetCount

    let source = (readJournal actorApi.System).AllEvents(Offset.Sequence(offsetCount))

    Log.Information("Journal started")
    let subQueue = Source.queue OverflowStrategy.Fail 1024
    let subSink = (Sink.broadcastHub 1024)

    let runnableGraph = subQueue |> Source.toMat subSink Keep.both

    let queue, subRunnable = runnableGraph |> Graph.run (actorApi.Materializer)

    source
    |> Source.recover (fun ex ->
        Log.Error(ex, "Error during event reading pipeline")
        None)
    |> Source.runForEach actorApi.Materializer (handleEventWrapper ctx actorApi queue)
    |> Async.StartAsTask
    |> ignore

    System.Threading.Thread.Sleep(1000)
    Log.Information("Projection init finished")
    subRunnable
