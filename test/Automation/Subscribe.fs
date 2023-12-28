module HolidayTracker.Automation.Subscribe

open Microsoft.Playwright
open TickSpec
open System.Threading.Tasks
open type Microsoft.Playwright.Assertions
open Authenticate
open System.Threading

[<Given>]
let ``I am authenticated`` (context: IBrowserContext) =
    (task {
        let page = ``I am not authenticated`` (context)
        Authenticate.``I sign in`` (page)
        return page
    })
        .Result

[<Given>]
let ``I am not subscribed to a country`` (context: IBrowserContext) = (task { return () }).Result


[<When>]
let ``I try to select a country`` (page: IPage) =
    (task {
        let switch = page.GetByRole(AriaRole.Switch).First
        do! page.WaitForLoadStateAsync()
        let! cookies = page.Context.CookiesAsync()

        let res =
            if cookies |> Seq.isEmpty then
                async { return () }
            else
                page.WaitForResponseAsync(fun u -> u.Url.Contains("/Subscribe"))
                |> Async.AwaitTask
                |> Async.Ignore

        do! switch.ClickAsync()
        do! res
        return ()
    })
        .Wait()

[<Then>]
let ``system should require me to login`` (page: IPage) =
    let exact = LocatorGetByTextOptions(Exact = true)

    (task {
        do!
            Expect(page.GetByRole(AriaRole.Dialog).GetByText("Sign in", exact))
                .ToBeVisibleAsync()
    })
        .Wait()

[<Then>]
let ``I should be subscribed to that country`` (page: IPage) =
    (task {
        let! _ = page.ReloadAsync()
        let firstSwitch = page.GetByRole(AriaRole.Switch).First
        do! Expect(firstSwitch).ToBeCheckedAsync()
    })
        .Result
