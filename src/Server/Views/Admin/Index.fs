module HolidayTracker.Server.Views.Admin.Index

open HolidayTracker.Server.Views.Common
open Microsoft.AspNetCore.Http

let view (env: _) (ctx: HttpContext) (dataLevel: int) =
    task {
        return
            html
                $"""
            <h2> Admin Page </h2>
            <a href="/admin/publish-event">Publish Event</a>
            <br/>
        """
    }
