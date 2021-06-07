[<AutoOpen>]
module SpeedySpa.Common

open Microsoft.AspNetCore.Http
open Falco

let bindJsonError (error: string) =
    Response.withStatusCode 400
    >> Response.ofPlainText (sprintf "Invalid JSON: %s" error)


/// Internal URLs
[<RequireQualifiedAccess>]
module Urls =
    let ``/`` = "/"
    let ``/auth/login`` = "/auth/login"
    let ``/auth/signup`` = "/auth/signup"

[<RequireQualifiedAccess>]
module Auth =
    open Falco.Security

    let requiresAuthentication (successHandler: HttpHandler) (failedAuthHandler: string -> HttpHandler) =
        // let d = dict ( seq { "1", 2 })
        // d.ContainsKey
        fun (ctx: HttpContext) ->
            if Auth.isAuthenticated ctx then
                successHandler ctx
            else
                failedAuthHandler "Authentication Failed" ctx

    let failedAuthResponse (error: string) =
        Response.withStatusCode 401
        >> Response.ofJson {| message = $"Request failed to authenticate: [%s{error}]" |}
