module HolidayTracker.Server.Views.Layout

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open System.Threading.Tasks
open Common

let scriptFiles =
    let assetsDir = "WebRoot/dist/assets"

    if Directory.Exists assetsDir then
        Directory.GetFiles(assetsDir, "*.js", SearchOption.AllDirectories)
    else
        [||]

let path =
    scriptFiles
    |> Array.map (fun x -> x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1))

let view (ctx: HttpContext) (env: _) (isDev) (body: int -> Task<string>) =
    task {
        let script =
            if isDev || path.Length = 0 then
                html
                    $"""
                <script type="module" src="/dist/@vite/client"></script>
                <script type="module" src="/dist/build/App.js"></script>
            """
            else
                let scripts =
                    path
                    |> Array.map (fun path ->
                        html
                            $"""
                    <script type="module" src="/dist/assets/{path}" ></script>
                    """)

                String.Join("\r\n", scripts)

        let! body = body 1
        let user = ctx.User.Identity.Name
        return
            html
                $"""
    <!DOCTYPE html>
    <html lang="en">
        <head>
            <meta charset="utf-8" >
            <base href="/" />
            <title> Alarms Global </title>

            <meta name="description"
                content="Alarms Global" />
            <meta name="keywords" content="Alarms Global">

            <link rel="apple-touch-icon" href="/assets/icons/icon-512.png">
            <!-- This meta viewport ensures the webpage's dimensions change according to the device it's on. This is called Responsive Web Design.-->
            <meta name="viewport"
                content="viewport-fit=cover, width=device-width, initial-scale=1.0" />
            <meta name="theme-color"  content="#181818" />

            <!-- These meta tags are Apple-specific, and set the web application to run in full-screen mode with a black status bar. Learn more at https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariHTMLRef/Articles/MetaTags.html-->
            <meta name="apple-mobile-web-app-capable" content="yes" />
            <meta name="apple-mobile-web-app-title" content="Alarms Global" />
            <meta name="apple-mobile-web-app-status-bar-style" content="black" />

            <!-- Imports an icon to represent the document. -->
            <link rel="icon" href="/assets/icons/icon-512.svg" type="image/x-icon" />

            <!-- Imports the manifest to represent the web application. A web app must have a manifest to be a PWA. -->
            <link rel="manifest" href="/manifest.webmanifest" />
            <link rel="stylesheet" href="/css/index.css?v=202307101701"/>

            <script defer crossorigin="anonymous" type="text/javascript" 
            src="https://cdnjs.cloudflare.com/ajax/libs/dompurify/3.0.1/purify.min.js"></script>
            <script nonce="110888888" src="https://accounts.google.com/gsi/client" async></script>
            <script defer src="/index.js"></script>
            {script}

        </head>
        <body>
            <header>
                <h1> Alarms Global </h1>
            </header>
            <main>
                <p> Current user: {user}</p>
                {body}
                <!-- Below is not used but required otherwise dynamic google sigin button won't appear-->
                    <div class="hidden">
                        <div id="g_id_onload"
                                data-client_id="961379412830-oe2516pvftiv91i2hga07u4n96vtu1lr.apps.googleusercontent.com"
                                data-context="signin"
                                data-ux_mode="popup"
                                data-login_uri="http://localhost:5070/signin-google"
                                data-auto_prompt="false"></div>
                    </div>
            </main>
            <footer>
                <a href="/privacy">Privacy Policy</a>
            </footer>
        </body>
    </html>"""
    }
