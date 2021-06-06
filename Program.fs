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

let razorEngine =
    RazorLightEngineBuilder()
        .UseFileSystemProject(Path.Combine(Environment.CurrentDirectory, "Pages"))
        .UseMemoryCachingProvider()
        .Build()

let Index : HttpHandler =
    fun ctx ->
        task {
            let! compiled = razorEngine.CompileTemplateAsync "Index.cshtml"
            let! result = razorEngine.RenderTemplateAsync(compiled, null)
            return! Response.ofHtmlString (result) ctx
        }

let UpdatableFragment : HttpHandler =
    fun ctx ->
        task {
            let! compiled = razorEngine.CompileTemplateAsync "Fragments/UpdatableFragment.cshtml"
            let! result = razorEngine.RenderTemplateAsync(compiled, null)
            return! Response.ofHtmlString result ctx
        }

let routes : HttpEndpoint list =
    [ get "/" Index
      get "/html/updatable-fragment" UpdatableFragment ]

// ------------
// Register services
// ------------
let configureServices (services: IServiceCollection) = services.AddFalco() |> ignore

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
