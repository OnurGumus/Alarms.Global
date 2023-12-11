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

let individuals = ctx.Main.Regions.Individuals.``Region_0809e16b-9904-4200-a31d-5e88c3875e59``
printf "%A" individuals
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

