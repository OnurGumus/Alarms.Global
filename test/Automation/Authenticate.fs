module HolidayTracker.Automation.Authenticate

open Microsoft.Playwright
open TickSpec
open System.Threading.Tasks
open type Microsoft.Playwright.Assertions

[<When>]
let ``I sign in`` (page: IPage) =
    (task {
        do! page.GetByRole(AriaRole.Button).GetByText("Sign In").First.ClickAsync()
        let dialog = page.GetByRole(AriaRole.Dialog)
        do! dialog.GetByPlaceholder("User name").FillAsync("onur@outlook.com.tr")
        do! dialog.GetByText("Sign in test user").ClickAsync()
    })
        .Wait()

[<Then>]
let ``I should be signed in`` (page: IPage) =
    (task { do! Expect(page.GetByRole(AriaRole.Button).GetByText("Sign out")).ToBeVisibleAsync() })
        .Wait()

[<Given>]
let ``I am signed in`` (context: IBrowserContext) =
    let page = Subscribe.``I am not authenticated`` (context)
    ``I sign in`` (page)
    page

[<When>]
let ``I sign out`` (page: IPage) =
    (task { do! page.GetByRole(AriaRole.Button).GetByText("Sign Out").First.ClickAsync() })
        .Wait()

[<Then>]
let ``I should be signed out`` (page: IPage) =
    (task { do! Expect(page.GetByRole(AriaRole.Button).GetByText("Sign in")).ToBeVisibleAsync() })
        .Wait()
