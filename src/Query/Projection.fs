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
    @"Data Source=" + __SOURCE_DIRECTORY__ + @"/../Server/Database/HolidayTracker.db;"

#else

[<Literal>]
let connectionString = @"Data Source=" + @"Database/HolidayTracker.db;"

#endif

[<Literal>]
let connectionStringReal = @"Data Source=" + @"Database/HolidayTracker.db;"

type Sql =
    SqlDataProvider<DatabaseProviderTypes.SQLITE, SQLiteLibrary=SQLiteLibrary.MicrosoftDataSqlite, 
    ConnectionString=connectionString, ResolutionPath=resolutionPath,
    ContextSchemaPath=schemaLocation,
    CaseSensitivityChange=CaseSensitivityChange.ORIGINAL>

let ctx = Sql.GetDataContext(connectionString)
QueryEvents.SqlQueryEvent |> Event.add (fun query -> Log.Debug ("Executing SQL {query}:", query))

let conn = ctx.CreateConnection()
conn.Open()
let cmd = conn.CreateCommand() 
cmd.CommandText <- "PRAGMA journal_mode=WAL;"
cmd.ExecuteNonQuery() |> ignore
conn.Dispose() |> ignore

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
let handleEventWrapper (connectionString: string) (actorApi:IActor) (subQueue: ISourceQueue<_>) (envelop: EventEnvelope) =
    try
       failwith "not implemented"
    with ex ->
        Log.Error(ex, "Error during event handling")
        actorApi.System.Terminate().Wait()
        Log.CloseAndFlush()
        System.Environment.Exit(-1)

let readJournal system =
    PersistenceQuery
        .Get(system)
        .ReadJournalFor<Akka.Persistence.Sql.Query.SqlReadJournal>(SqlReadJournal.Identifier)

let init (connectionString: string) (actorApi: IActor) =
    Log.Information("init query side")
    let offsetCount = 0
    
    let source =
        (readJournal actorApi.System).AllEvents(Offset.Sequence(offsetCount))
    
    Log.Information("Journal started")
    let subQueue = Source.queue OverflowStrategy.Fail 1024
    let subSink = (Sink.broadcastHub 1024)

    let runnableGraph = subQueue |> Source.toMat subSink Keep.both

    let queue, subRunnable = runnableGraph |> Graph.run (actorApi.Materializer)
    source
    |> Source.recover (fun ex ->
        Log.Error(ex, "Error during event reading pipeline")
        None)
    |> Source.runForEach actorApi.Materializer (handleEventWrapper connectionString actorApi queue)
    |> Async.StartAsTask
    |> ignore
    
    System.Threading.Thread.Sleep(1000)
    Log.Information("Projection init finished")
    subRunnable