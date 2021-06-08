[<AutoOpen>]
module SpeedySpa.Common

open Microsoft.AspNetCore.Http

open FSharp.Control.Tasks

open Falco

open SpeedySpa.Types
open SpeedySpa.Types.ServiceTypes
open Microsoft.AspNetCore.Antiforgery

[<RequireQualifiedAccess>]
module Urls =

    [<Literal>]
    let ``/`` = "/"

    [<Literal>]
    let ``/auth/login`` = "/auth/login"

    [<Literal>]
    let ``/auth/signup`` = "/auth/signup"


let csrfFailedHandler (route: RouteKind) (ctx: HttpContext) =
    task {
        let tpl = ctx.GetService<TemplateProvider>()
        let antiforgery = ctx.GetService<IAntiforgery>()
        let tokens = antiforgery.GetAndStoreTokens ctx

        let vm =
            { csrfToken = tokens.RequestToken
              errors =
                  [ "GeneralError", "Invalid Security Token In The Request, Please Try Again" ]
                  |> Map.ofList }

        let template =
            match route with
            | RouteKind.Login -> "Fragments/LoginForm.cshtml"
            | RouteKind.Signup -> "Fragments/SignupForm.cshtml"

        let! result = tpl.getTemplate (template, Some vm)
        return! Response.ofHtmlString result ctx
    }
