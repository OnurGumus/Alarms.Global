module HolidayTracker.Server.HTTP

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.StaticFiles

let scriptSrcElem =
    [|
        """data:"""
        """'nonce-110888888'"""
        """https://cdnjs.cloudflare.com/ajax/libs/dompurify/"""
        """https://accounts.google.com/"""    
        |]
    |> String.concat " "

let styleSrcWithHashes = [| """'nonce-110888888'""" |] |> String.concat " "

let styleSrc =
    [| 
        """https://unpkg.com/open-props@1.6.13/"""
        """https://accounts.google.com/""" 
        """'nonce-110888888'"""
    |]
    |> String.concat " "

let styleSrcElem =
    [|
        """https://unpkg.com/open-props@1.6.13/"""
        """https://accounts.google.com/""" 
        """'nonce-110888888'"""
    |]
    |> String.concat " "

let headerMiddleware =
    fun (context: HttpContext) (next: Func<Task>) ->
        let headers = context.Response.Headers
        headers.Add("X-Content-Type-Options", "nosniff")

        match context.Request.Headers.TryGetValue("Accept") with
        // | true, accept ->
        //     if accept |> Seq.exists (fun x -> x.Contains "text/html") then
        //         headers.Add("Cross-Origin-Embedder-Policy", "corp")
        //         headers.Add("Cross-Origin-Opener-Policy", "same-origin")

            // headers.Add(
            //     "Content-Security-Policy-Report-Only",
            //     $"default-src *;\
            // font-src 'self';\
            // img-src 'self';\
            // manifest-src 'self';\
            // script-src-elem 'self' {scriptSrcElem} ;\
            // connect-src 'self' localhost ws://192.168.50.236:* ws://localhost:* http://localhost:*/dist/ https://localhost:*/dist/;\
            // style-src * ;\
            // style-src-elem 'self' {styleSrcElem} ;\
            // worker-src 'self';\
            // form-action 'self';\
            // script-src  'wasm-unsafe-eval';\
            // frame-src 'self';\
            // require-trusted-types-for 'script';\
            // trusted-types * 'allow-duplicates';\
            // "
            // )
        | _ -> ()

        next.Invoke()


let provider = FileExtensionContentTypeProvider()

provider.Mappings[".css"] <- "text/css; charset=utf-8"
provider.Mappings[".js"] <- "text/javascript; charset=utf-8"
provider.Mappings[".webmanifest"] <- "application/manifest+json; charset=utf-8"

let staticFileOptions =
    StaticFileOptions(
        ContentTypeProvider = provider,
        OnPrepareResponse =
            fun (context) ->
#if !DEBUG
                let headers = context.Context.Response.GetTypedHeaders()

                headers.CacheControl <-
                    Microsoft.Net.Http.Headers.CacheControlHeaderValue(Public = true, MaxAge = TimeSpan.FromDays(365))
#else
                ()
#endif

    )
