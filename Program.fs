module SpeedySpa.Program

open System
open System.IO

open Falco
open Falco.Routing
open Falco.HostBuilder

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.Extensions.DependencyInjection

open SpeedySpa.Types.ServiceTypes
open SpeedySpa.Services

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

    services.AddAuthorization() |> ignore
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
        .UseAuthentication()
        .UseAuthorization()
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
        endpoints Routes.routes
    }

    0
