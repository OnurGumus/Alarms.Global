module DB

open FluentMigrator
open System
open Microsoft.Extensions.DependencyInjection
open FluentMigrator.Runner
open Microsoft.Extensions.Configuration
open System.Collections.Generic
open HolidayTracker.Shared.Model
open Thoth.Json.Net

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
            // clean up akka stuff
            this.Execute.Sql("DELETE FROM SNAPSHOT") |> ignore
            this.Execute.Sql("DELETE FROM EVENT_JOURNAL") |> ignore
            this.Execute.Sql("DELETE FROM JOURNAL_METADATA") |> ignore
        with _ ->
            ()

let regions =
    [ "Argentina"; "Brazil"; "Canada"; "Denmark"; "France" ]
    |> List.map (fun name -> {
        RegionId = RegionId.CreateNew()
        AlrernateNames = []
        RegionType = Country
        Name = name |> ShortString.TryCreate |> forceValidate
    })
    |> List.ofSeq

[<MigrationAttribute(2023_12_11_1340L)>]
type AddRegions() =
    inherit Migration()

    override this.Up() = 
        this.Create
            .Table("Regions")
            .WithColumn("RegionId").AsString().PrimaryKey()
            .WithColumn("Name").AsString().Indexed()
            .WithColumn("Type").AsString()
            .WithColumn("AlternateNames").AsString()
        |> ignore
        for region in regions do
            let row:IDictionary<string,obj> = 
                let l = [
                    ("RegionId", region.RegionId.Value.Value:>obj)
                    "Name", region.Name.Value
                    "Type",  region.RegionType.ToString()
                    "AlternateNames", (region.AlrernateNames |> encode) :>obj
                ]
                Map.ofSeq l
            this.Insert.IntoTable("Regions").Row(row) |> ignore


    override this.Down() =  this.Delete.Table("Specials") |> ignore
       

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
