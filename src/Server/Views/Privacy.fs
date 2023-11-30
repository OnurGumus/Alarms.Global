module HolidayTracker.Server.Views.Privacy

open Common
open Microsoft.AspNetCore.Http

let view (env: _) (ctx: HttpContext) (dataLevel: int) =
    task {
        return
            html
                $"""
            <h{dataLevel + 1}>Privacy Policy</h{dataLevel + 1}>
            <p>
                This is the privacy policy for the Holiday Tracker application.
            </p>
        """
    }
