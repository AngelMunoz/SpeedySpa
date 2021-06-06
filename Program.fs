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

// Load RazorLignt just to show the idea
let razorEngine =
    RazorLightEngineBuilder()
        .UseFileSystemProject(Path.Combine(Environment.CurrentDirectory, "Pages"))
        .UseMemoryCachingProvider()
        .Build()

/// This is the normal page stuff, an indes file which is compiled then rendered
let Index : HttpHandler =
    fun ctx ->
        task {
            let! compiled = razorEngine.CompileTemplateAsync "Index.cshtml"
            let! result = razorEngine.RenderTemplateAsync(compiled, null)
            return! Response.ofHtmlString (result) ctx
        }

/// This page is different (it's still HTML and all of that) instead of loading the whole
/// layout with it, we just need to load a partial so we'd send JSON normaly here we send the HTML
/// as the one we'd like to see rendered already
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
