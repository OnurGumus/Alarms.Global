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