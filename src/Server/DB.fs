module DB

open FluentMigrator
open System
open Microsoft.Extensions.DependencyInjection
open FluentMigrator.Runner
open Microsoft.Extensions.Configuration
open System.Collections.Generic

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