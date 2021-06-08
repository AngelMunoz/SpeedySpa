[<RequireQualifiedAccess>]
module SpeedySpa.Routes

open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Falco

open SpeedySpa
open SpeedySpa.Types
open SpeedySpa.Types.ServiceTypes
open SpeedySpa.Handlers
open Falco.Routing
open Microsoft.AspNetCore.Antiforgery

let getIndex : HttpHandler =
    fun ctx -> Home.IndexPage(ctx.GetService<TemplateProvider>()) ctx

let getLogin : HttpHandler =
    fun ctx -> Auth.LoginPage(ctx.GetService<TemplateProvider>()) ctx


let postLogin : HttpHandler =
    Request.bindFormSecure
        Auth.LoginFormBinder
        (fun payload ctx -> Auth.Login(ctx.GetService<TemplateProvider>()) (ctx.GetService<IAntiforgery>()) payload ctx)
        (fun errors ctx ->
            Auth.FormBindErrorHandler
                RouteKind.Login
                (ctx.GetService<TemplateProvider>())
                (ctx.GetService<IAntiforgery>())
                errors
                ctx)
        (csrfFailedHandler RouteKind.Login)

let postSignup : HttpHandler =
    Request.bindFormSecure
        Auth.SignupFormBinder
        (fun payload ctx ->
            Auth.Signup(ctx.GetService<TemplateProvider>()) (ctx.GetService<IAntiforgery>()) payload ctx)
        (fun errors ctx ->
            Auth.FormBindErrorHandler
                RouteKind.Signup
                (ctx.GetService<TemplateProvider>())
                (ctx.GetService<IAntiforgery>())
                errors
                ctx)
        (csrfFailedHandler RouteKind.Signup)


let routes : HttpEndpoint list =
    [ get Urls.``/`` getIndex
      get Urls.``/auth/login`` getLogin
      post Urls.``/auth/login`` postLogin
      post Urls.``/auth/signup`` postSignup ]
