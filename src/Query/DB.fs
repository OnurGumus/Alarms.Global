module DB

open FluentMigrator
open System
open Microsoft.Extensions.DependencyInjection
open FluentMigrator.Runner
open Microsoft.Extensions.Configuration
open System.Collections.Generic
open HolidayTracker.Shared.Model
open Thoth.Json.Net
open Subscription

let extraThoth = Extra.empty |> Extra.withInt64 |> Extra.withDecimal

let inline encode<'T> =
    Encode.Auto.generateEncoderCached<'T> (caseStrategy = CamelCase, extra = extraThoth)
    >> Encode.toString 0

let inline decode<'T> =
    Decode.Auto.generateDecoderCached<'T> (caseStrategy = CamelCase, extra = extraThoth)
    |> Decode.fromString

[<MigrationAttribute(1L)>]
type Zero() =
    inherit Migration()

    override this.Up() = ()

    override this.Down() = ()

[<MigrationAttribute(2L)>]
type One() =
    inherit Migration()

    override this.Up() = ()

    override this.Down() =
        try
            if this.Schema.Table("snapshot").Exists() then
                // clean up akka stuff
                this.Execute.Sql("DELETE FROM snapshot")
                this.Execute.Sql("DELETE FROM JOURNAL")
                this.Execute.Sql("DELETE FROM SQLITE_SEQUENCE")
                this.Execute.Sql("DELETE FROM TAGS")
        with _ ->
            ()

let regions =
    [ "Argentina"; "Brazil"; "Canada"; "Denmark"; "France" ]
    |> List.map (fun name ->
        { RegionId = RegionId.CreateNew()
          AlrernateNames = []
          RegionType = Country
          Name = name |> ShortString.TryCreate |> forceValidate })
    |> List.ofSeq

[<MigrationAttribute(2023_12_11_1340L)>]
type AddRegions() =
    inherit Migration()

    override this.Up() =
        this.Create
            .Table("Regions")
            .WithColumn("RegionId")
            .AsString()
            .PrimaryKey()
            .WithColumn("Name")
            .AsString()
            .Indexed()
            .WithColumn("Type")
            .AsString()
            .WithColumn("AlternateNames")
            .AsString()
        |> ignore

        for region in regions do
            let row: IDictionary<string, obj> =
                let l =
                    [ ("RegionId", region.RegionId.Value.Value :> obj)
                      "Name", region.Name.Value
                      "Type", region.RegionType.ToString()
                      "AlternateNames", (region.AlrernateNames |> encode) :> obj ]

                Map.ofSeq l

            this.Insert.IntoTable("Regions").Row(row) |> ignore


    override this.Down() = this.Delete.Table("Regions") |> ignore

[<MigrationAttribute(2023_12_11_2101L)>]
type AddOffsetsTable() =
    inherit Migration()

    override this.Up() =
        this.Create
            .Table("Offsets")
            .WithColumn("OffsetName")
            .AsString()
            .PrimaryKey()
            .WithColumn("OffsetCount")
            .AsInt64()
            .NotNullable()
            .WithDefaultValue(0)
        |> ignore

        let dict: IDictionary<string, obj> = Dictionary()
        dict.Add("OffsetName", "HolidayTracker")
        dict.Add("OffsetCount", 0L)

        this.Insert.IntoTable("Offsets").Row(dict) |> ignore

    override this.Down() = this.Delete.Table("Offsets") |> ignore

[<MigrationAttribute(2023_12_13_2207L)>]
type AddUserIdentityTable() =
    inherit Migration()

    override this.Up() =
        this.Create
            .Table("UserIdentities")
            .WithColumn("Identity")
            .AsString()
            .PrimaryKey()
            .WithColumn("ClientId")
            .AsString()
            .Indexed()
            .WithColumn("Version")
            .AsInt64()
        |> ignore

    override this.Down() =
        this.Delete.Table("UserIdentities") |> ignore


[<MigrationAttribute(2023_12_20_1553L)>]
type AddUserSubscriptionsTable() =
    inherit Migration()

    override this.Up() =
        this.Create
            .Table("Subscriptions")
            .WithColumn("Identity")
            .AsString()
            .PrimaryKey()
            .WithColumn("RegionId")
            .AsString()
            .PrimaryKey()
        |> ignore

    override this.Down() =
        this.Delete.Table("Subscriptions") |> ignore


[<MigrationAttribute(2023_12_20_1554L)>]
type AddUserSettingsTable() =
    inherit Migration()

    override this.Up() =
        this.Create
            .Table("UserSettings")
            .WithColumn("Identity")
            .AsString()
            .PrimaryKey()
            .WithColumn("ReminderDays")
            .AsInt32()
            .WithColumn("Version")
            .AsInt64()
        |> ignore

    override this.Down() =
        this.Delete.Table("UserSettings") |> ignore


[<MigrationAttribute(2023_12_29_2033L)>]
type AddUserGlobalEventsTable() =
    inherit Migration()

    override this.Up() =
        this.Create
            .Table("GlobalEvents")
            .WithColumn("Id")
            .AsString()
            .PrimaryKey()
            .WithColumn("Title")
            .AsString()
            .WithColumn("Body")
            .AsString()
            .WithColumn("RegionIds")
            .AsString()
            .WithColumn("TargetDate")
            .AsDateTime()
            .Nullable()
        |> ignore

    override this.Down() =
        this.Delete.Table("GlobalEvents") |> ignore

let updateDatabase (serviceProvider: IServiceProvider) =
    let runner = serviceProvider.GetRequiredService<IMigrationRunner>()
    runner.MigrateUp()

let resetDatabase (serviceProvider: IServiceProvider) =
    let runner = serviceProvider.GetRequiredService<IMigrationRunner>()

    if runner.HasMigrationsToApplyRollback() then
        runner.RollbackToVersion(1L)

let createServices (config: IConfiguration) =
    let connString =
        config.GetSection(HolidayTracker.Shared.Constants.ConnectionString).Value

    ServiceCollection()
        .AddFluentMigratorCore()
        .ConfigureRunner(fun rb ->
            rb
                .AddSQLite()
                .WithGlobalConnectionString(connString)
                .ScanIn(typeof<Zero>.Assembly)
                .For.Migrations()
            |> ignore)
        .AddLogging(fun lb -> lb.AddFluentMigratorConsole() |> ignore)
        .BuildServiceProvider(false)

let init (env: _) =
    let config = env :> IConfiguration
    use serviceProvider = createServices config
    use scope = serviceProvider.CreateScope()
    updateDatabase scope.ServiceProvider

let reset (env: _) =
    let config = env :> IConfiguration
    use serviceProvider = createServices config
    use scope = serviceProvider.CreateScope()
    resetDatabase scope.ServiceProvider
    init env
