module SpeedySpa.Routes

open System.Security.Claims
open System.Threading.Tasks

open Falco


open FSharp.Control.Tasks

open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http

open SpeedySpa
open SpeedySpa.Types
open SpedySpa.Services

let private getSignInClaims (email: string) =
    [ Claim(ClaimTypes.Name, email)
      Claim(ClaimTypes.Email, email) ]

module private Actions =

    type ManagedError =
        | AlreadyExists
        | EmptyValues
        | FailedToCreate
        | DatabaseError

    [<RequireQualifiedAccess>]
    module User =
        open type BCrypt.Net.BCrypt

        let login (user: LoginPayload) =
            task {
                match! Users.TryFindByEmailWithPassword user.email with
                | Some found -> return EnhancedVerify(user.password, found.password)
                | None -> return false
            }

        let signup (user: SignupPayload) : Task<Result<User, ManagedError>> =
            task {
                let! exists = Users.Exists user.email

                if exists then
                    return Error AlreadyExists
                else

                    let! user =
                        Users.TryCreate
                            { user with
                                  password = EnhancedHashPassword user.password }

                    match user with
                    | None -> return Error FailedToCreate
                    | Some user -> return Ok user

            }

open Actions
open Microsoft.AspNetCore.Antiforgery

let private loginHandler (payload: LoginPayload) (ctx: HttpContext) =
    task {
        let! loggedIn = User.login payload

        if not loggedIn then
            let tpl = ctx.GetService<TemplateProvider>()
            let antiforgery = ctx.GetService<IAntiforgery>()
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

            return! Response.signInAndRedirect CookieAuthenticationDefaults.AuthenticationScheme claims Urls.``/`` ctx
    }

let signUpHandler (payload: SignupPayload) (ctx: HttpContext) =
    task {
        let! signedUp = User.signup payload

        match signedUp with
        | Ok user ->
            let claims =
                let claims = getSignInClaims payload.email

                let identity =
                    ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)

                ClaimsPrincipal identity

            return!
                Response.signInAndRedirect
                    CookieAuthenticationDefaults.AuthenticationScheme
                    claims
                    Urls.``/auth/login``
                    ctx
        | Error apierror ->
            let tpl = ctx.GetService<TemplateProvider>()

            let tokens =
                (ctx.GetService<IAntiforgery>())
                    .GetAndStoreTokens ctx

            let vm =
                { csrfToken = tokens.RequestToken
                  errors = Map.empty }

            let! result =
                match apierror with
                | AlreadyExists ->
                    let vm =
                        { vm with
                              errors =
                                  Map.empty
                                  |> Map.add "email" "Email Already Exists" }

                    tpl.getTemplate<AuthFormViewModel> ("Fragments/SignupForm.cshtml", Some vm)
                | EmptyValues ->
                    let vm =
                        { vm with
                              errors =
                                  Map.empty
                                  |> Map.add "MissingValues" "There are some missing values in the request" }

                    tpl.getTemplate<AuthFormViewModel> (
                        "Fragments/SignupForm.cshtml",
                        Some vm

                    )
                | DatabaseError
                | FailedToCreate ->
                    let vm =
                        { vm with
                              errors =
                                  Map.empty
                                  |> Map.add "GeneralError" "Something went wrong at our side" }

                    tpl.getTemplate<AuthFormViewModel> (
                        "Fragments/SignupForm.cshtml",
                        Some vm

                    )

            return! Response.ofHtmlString result ctx
    }

let indexHandler (tpl: TemplateProvider) (ctx: HttpContext) =
    task {
        let isAuthenticated = Falco.Security.Auth.isAuthenticated ctx

        if isAuthenticated |> not then
            return! Response.redirect Urls.``/auth/login`` false ctx
        else
            let! index = tpl.getTemplate ("Index.cshtml", None)
            return! Response.ofHtmlString index ctx
    }

let getLoginHandler (tpl: TemplateProvider) (ctx: HttpContext) =
    task {
        let antiforgery = ctx.GetService<IAntiforgery>()
        let tokens = antiforgery.GetAndStoreTokens ctx

        let vm =
            { csrfToken = tokens.RequestToken
              errors = dict Seq.empty }

        let! index = tpl.getTemplate ("Login.cshtml", Some vm)
        return! Response.ofHtmlString index ctx
    }

let getLogin : HttpHandler =
    fun ctx -> getLoginHandler (ctx.GetService<TemplateProvider>()) ctx

let getIndex : HttpHandler =
    fun ctx -> indexHandler (ctx.GetService<TemplateProvider>()) ctx

let postLogin : HttpHandler =
    Request.bindFormSecure
        (fun binder ->
            match binder.TryGetStringNonEmpty "email", binder.TryGetStringNonEmpty "password" with
            | Some email, Some password -> Ok { email = email; password = password }
            | _ -> Error "Missing Fields")
        loginHandler
        bindJsonError
        (bindJsonError "Failed to Validate CSRF Token")


let postSignup : HttpHandler =
    Request.bindJson signUpHandler bindJsonError
