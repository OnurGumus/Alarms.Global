module HolidayTracker.Automation.Setup

open HolidayTracker
open Microsoft.Playwright
open TickSpec
open Microsoft.Extensions.Hosting
open System.Threading.Tasks
open System
open Microsoft.Extensions.Configuration
open Hocon.Extensions.Configuration
open System.IO
open HolidayTracker.Server

let configBuilder =
    ConfigurationBuilder()
        .AddHoconFile("test-config.hocon")
        .AddEnvironmentVariables()

let config = configBuilder.Build()

Directory.SetCurrentDirectory("/workspaces/HolidayTracker/src/Server")

let appEnv = Environments.AppEnv(config)

let host = App.host appEnv [||]

host.Start()

let playwright = Playwright.CreateAsync().Result
let browser = playwright.Chromium.LaunchAsync().Result

[<BeforeScenario>]
let setupContext() =
    let context = 
        browser
            .NewContextAsync(BrowserNewContextOptions(IgnoreHTTPSErrors = true))
            .Result
    context

[<AfterScenario>]
let afterContext () =
    appEnv.Reset()