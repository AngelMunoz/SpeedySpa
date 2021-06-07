module SpeedySpa.Program

open System
open System.IO

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks
open RazorLight
open System.Collections.Generic
open System.Diagnostics
open SpedySpa.Services
open Microsoft.AspNetCore.Authentication.Cookies

let routes : HttpEndpoint list =
    [ get Urls.``/`` Routes.getIndex
      get Urls.``/auth/login`` Routes.getLogin
      post Urls.``/auth/login`` Routes.postLogin
      post Urls.``/auth/signup`` Routes.postSignup ]

// ------------
// Register services
// ------------
let configureServices (services: IServiceCollection) : unit =
    services
        .AddFalco()
        .AddScoped<TemplateProvider>(fun _ -> tplProvider)
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie()
    |> ignore

    services.AddAntiforgery() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (endpoints: HttpEndpoint list) (ctx: WebHostBuilderContext) (app: IApplicationBuilder) =
    let devMode =
        StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"

    app
        .UseWhen(devMode, (fun app -> app.UseDeveloperExceptionPage()))
        .UseWhen(
            not (devMode),
            fun app ->
                app.UseFalcoExceptionHandler(
                    Response.withStatusCode 500
                    >> Response.ofPlainText "Server error"
                )
        )
        .UseFalco(endpoints)
        .UseStaticFiles()
    |> ignore

// -----------
// Configure Web host
// -----------
let configureWebHost (endpoints: HttpEndpoint list) (webHost: IWebHostBuilder) =
    webHost
        .ConfigureServices(configureServices)
        .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =
    webHost args {
        configure configureWebHost
        endpoints routes
    }

    0
