﻿module HolidayTracker.Server.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.SerilogExtensions
open Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Serilog
open Hocon.Extensions.Configuration
open HolidayTracker.Server.Views
open System.Globalization
open HolidayTracker.Server.Views
open HolidayTracker.Server.Handlers.Default
open HTTP

CultureInfo.DefaultThreadCurrentCulture <- CultureInfo.InvariantCulture
CultureInfo.DefaultThreadCurrentUICulture <- CultureInfo.InvariantCulture

bootstrapLogger ()

type Self = Self

let errorHandler (ex: Exception) (ctx: HttpContext) =
    Log.Error(ex, "Error Handler")

    match ex with
    | :? System.Text.Json.JsonException -> clearResponse >=> setStatusCode 400 >=> text ex.Message
    | _ -> clearResponse >=> setStatusCode 500 >=> text ex.Message

let configureCors (builder: CorsPolicyBuilder) =
#if DEBUG
    builder
        .WithOrigins("http://localhost:5070", "https://localhost:5071")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore
#else
    ()
#endif

let configureApp (app: IApplicationBuilder, appEnv) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    let isDevelopment = env.IsDevelopment()

    let app = if isDevelopment then app else app.UseResponseCompression()

    app
        .UseDefaultFiles()
        .UseAuthentication()
        .UseAuthorization()
        .UseMiddleware<LogUserNameMiddleware>()
        .Use(headerMiddleware)
    |> ignore

    let layout ctx =
        Layout.view ctx (appEnv) (env.IsDevelopment())

    let webApp = webAppWrapper appEnv layout
    let sConfig = Serilog.configure errorHandler
    let handler = SerilogAdapter.Enable(webApp, sConfig)

    (match isDevelopment with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles(staticFileOptions)
        // .UseThrottlingTroll(Throttling.setOptions)
        .UseWebSockets()
        .UseGiraffe(handler)

    if env.IsDevelopment() then
        app.UseSpa(fun spa ->
            let path = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../.")
            spa.Options.SourcePath <- path
            spa.Options.DevServerPort <- 5173
            spa.UseReactDevelopmentServer(npmScript = "watch"))

        app.UseSerilogRequestLogging() |> ignore

let configureServices (services: IServiceCollection) =
    services
        .AddAuthorization()
        .AddResponseCompression(fun options -> options.EnableForHttps <- true)
        .AddCors()
        .AddGiraffe()
        .AddAntiforgery()
#if !DEBUG
        //   .AddApplicationInsightsTelemetry()
#endif
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(
            CookieAuthenticationDefaults.AuthenticationScheme,
            fun options ->
                options.SlidingExpiration <- true
                options.ExpireTimeSpan <- TimeSpan.FromDays(7)
        )
    |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

let host appEnv args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    let host =
        Host
            .CreateDefaultBuilder(args)
            .UseSerilog(Serilog.configureMiddleware)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
#if !DEBUG
                    .UseEnvironment(Environments.Production)
#else
                    .UseEnvironment(Environments.Development)
#endif
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder>(fun builder -> configureApp (builder, appEnv)))
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                |> ignore)
            .Build()

    host

[<EntryPoint>]
let main args =

    let configBuilder =
        ConfigurationBuilder()
            .AddUserSecrets<Self>()
            .AddHoconFile("config.hocon")
            .AddHoconFile("secrets.hocon", true)
            .AddEnvironmentVariables()

    let config = configBuilder.Build()

    let mutable ret = 0

    try
        try
            let appEnv = new Environments.AppEnv(config)
            appEnv.Init()

            (host appEnv args).Run()
        with ex ->
            Log.Fatal(ex, "Host terminated unexpectedly")
            ret <- -1
    finally
        Log.CloseAndFlush()

    ret
