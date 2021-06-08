namespace SpeedySpa.Handlers

open System.Security.Claims
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http

open Falco

open SpeedySpa
open SpeedySpa.Types
open SpeedySpa.Types.ServiceTypes
open SpeedySpa.Services


[<RequireQualifiedAccess>]
module Auth =

    let private getSignInClaims (email: string) =
        [ Claim(ClaimTypes.Name, email)
          Claim(ClaimTypes.Email, email) ]

    let LoginFormBinder (binder: FormCollectionReader) : Result<LoginPayload, Map<string, string>> =
        match binder.TryGetStringNonEmpty "email", binder.TryGetStringNonEmpty "password" with
        | Some email, Some password -> Ok { email = email; password = password }
        | None, None ->
            Error(
                [ "email", "Email can't be empty"
                  "password", "Password can't be empty" ]
                |> Map.ofList
            )
        | Some _, None ->
            Error(
                [ "password", "Password can't be empty" ]
                |> Map.ofList
            )
        | None, Some _ -> Error([ "email", "Email can't be empty" ] |> Map.ofList)

    let SignupFormBinder (binder: FormCollectionReader) : Result<SignupPayload, Map<string, string>> =
        let name = binder.TryGetStringNonEmpty "name"
        let email = binder.TryGetStringNonEmpty "email"
        let password = binder.TryGetStringNonEmpty "password"

        let repeatPassword =
            binder.TryGetStringNonEmpty "repeatPassword"

        match name, email, password, repeatPassword with
        | Some name, Some email, Some password, Some repeatPassword when password = repeatPassword ->
            Ok
                { name = name
                  password = password
                  email = email }
        | Some _, Some _, Some password, Some repeatPassword when password <> repeatPassword ->
            Error(
                [ "password", "The passwords must match"
                  "repeatPassword", "The passwords must match" ]
                |> Map.ofList
            )
        | _ ->
            Error(
                Map.empty
                |> Map.add "GeneralError" "There are missing fields in the form"
            )

    let FormBindErrorHandler
        (route: RouteKind)
        (tpl: TemplateProvider)
        (antiforgery: IAntiforgery)
        (errors: Map<string, string>)
        (ctx: HttpContext)
        =
        task {
            let tokens = antiforgery.GetAndStoreTokens ctx

            let vm =
                { csrfToken = tokens.RequestToken
                  errors = errors }

            let! template =
                let template =
                    match route with
                    | RouteKind.Login -> "Fragments/LoginForm.cshtml"
                    | RouteKind.Signup -> "Fragments/SignupForm.cshtml"

                tpl.getTemplate<AuthFormViewModel> (template, Some vm)

            return! Response.ofHtmlString template ctx
        }


    let Login (tpl: TemplateProvider) (antiforgery: IAntiforgery) (payload: LoginPayload) (ctx: HttpContext) =
        task {
            let! loggedIn = Users.CanLogin payload

            if not loggedIn then
                let tokens = antiforgery.GetAndStoreTokens ctx

                let vm =
                    { csrfToken = tokens.RequestToken
                      errors = Map.empty |> Map.add "email" "Invalid credentials" }

                let! template = tpl.getTemplate<AuthFormViewModel> ("Fragments/LoginForm.cshtml", Some vm)

                return! Response.ofHtmlString template ctx
            else
                let claims =
                    let claims = getSignInClaims payload.email

                    let identity =
                        ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)

                    ClaimsPrincipal identity

                return!
                    Response.signInAndRedirect CookieAuthenticationDefaults.AuthenticationScheme claims Urls.``/`` ctx
        }

    let Signup (tpl: TemplateProvider) (antiforgery: IAntiforgery) (payload: SignupPayload) (ctx: HttpContext) =
        task {
            let! result = Users.TrySignup payload

            match result with
            | Ok user ->
                let claims =
                    let claims = getSignInClaims user.email

                    let identity =
                        ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)

                    ClaimsPrincipal identity

                return!
                    Response.signInAndRedirect CookieAuthenticationDefaults.AuthenticationScheme claims Urls.``/`` ctx
            | Error apiError ->
                let tokens = antiforgery.GetAndStoreTokens ctx

                let vm =
                    { csrfToken = tokens.RequestToken
                      errors =
                          match apiError with
                          | SignupError.AlreadyExists ->
                              [ "email", "This email is already in use" ]
                              |> Map.ofList
                          | SignupError.CouldNotCreate ->
                              [ "GeneralError",
                                "There was a problem on our side, please contact support if this happens again" ]
                              |> Map.ofList
                          | SignupError.MissingField (key, value) -> [ key, value ] |> Map.ofList }

                let! template = tpl.getTemplate<AuthFormViewModel> ("Fragments/SignupForm.cshtml", Some vm)

                return! Response.ofHtmlString template ctx
        }

    let LoginPage (tpl: TemplateProvider) (ctx: HttpContext) =
        task {
            let antiforgery = ctx.GetService<IAntiforgery>()
            let tokens = antiforgery.GetAndStoreTokens ctx

            let vm =
                { csrfToken = tokens.RequestToken
                  errors = Map.empty }

            let! index = tpl.getTemplate ("Login.cshtml", Some vm)
            return! Response.ofHtmlString index ctx
        }
