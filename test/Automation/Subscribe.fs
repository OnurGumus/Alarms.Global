module HolidayTracker.Automation.Subscribe

open Microsoft.Playwright
open TickSpec
open System.Threading.Tasks
open type Microsoft.Playwright.Assertions

[<Given>]
let ``I am not authenticated`` (context: IBrowserContext) =
    (task {
        do! context.ClearCookiesAsync()
        do! Task.Delay 2000
        let! page = context.NewPageAsync()
        let! _ = page.GotoAsync("http://localhost:5070?test-user")
        return (page)
    })
        .Result

[<When>]
let ``I try to select a country`` (page: IPage) =
    (task { do! page.GetByRole(AriaRole.Switch).First.ClickAsync() }).Wait()

[<Then>]
let ``system should require me to login`` (page: IPage) =
    let exact = LocatorGetByTextOptions(Exact = true)

    (task {
        do!
            Expect(page.GetByRole(AriaRole.Dialog).GetByText("Sign in", exact))
                .ToBeVisibleAsync()
    })
        .Wait()
