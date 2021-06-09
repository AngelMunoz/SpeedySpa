namespace SpeedySpa.Handlers

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks

open Falco
open SpeedySpa
open SpeedySpa.Types
open SpeedySpa.Types.ServiceTypes


[<RequireQualifiedAccess>]
module Home =
    let IndexPage (tpl: TemplateProvider) (ctx: HttpContext) =
        task {
            let isAuthenticated = Falco.Security.Auth.isAuthenticated ctx

            if isAuthenticated then
                let! index = tpl.getTemplate ("Index.cshtml", Some { nav = None })
                return! Response.ofHtmlString index ctx
            else
                return! Response.redirect Urls.``/auth/login`` false ctx
        }
