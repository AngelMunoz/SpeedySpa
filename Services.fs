module SpeedySpa.Services

open System
open System.IO
open System.Threading.Tasks
open FSharp.Control.Tasks

open RazorLight

open MongoDB.Bson
open MongoDB.Driver

open Mondocks.Queries
open Mondocks.Aggregation
open Mondocks.Types

open SpeedySpa.Types
open SpeedySpa.Types.ServiceTypes
open SpeedySpa.Database
open FsToolkit.ErrorHandling

open type BCrypt.Net.BCrypt
open System.Text.RegularExpressions

let tplEngine =
    lazy
        (RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(Environment.CurrentDirectory, "Pages"))
            .UseMemoryCachingProvider()
            .Build())

let tplProvider =
    { new TemplateProvider with

        override this.getTemplate<'T>(key: string, ?model: 'T) : Task<string> =
            task {
                let! template = tplEngine.Value.CompileTemplateAsync key

                let! result =
                    match model with
                    | Some model -> tplEngine.Value.RenderTemplateAsync(template, model)
                    | None -> tplEngine.Value.RenderTemplateAsync(template, null)

                return result
            } }


[<RequireQualifiedAccess>]
module Users =

    let TryFindByEmail (email: string) : Task<Option<User>> =
        task {
            let findOne =
                find UsersColName {
                    filter {| email = email |}
                    limit 1
                }

            try
                let! result = database.Value.RunCommandAsync<FindResult<User>>(JsonCommand findOne)

                return result.cursor.firstBatch |> Seq.tryHead
            with ex ->
                eprintfn $"TryFindByEmail: [{ex.Message}]"
                return None
        }

    let TryFindByEmailWithPassword
        (email: string)
        : Task<Option<{| _id: ObjectId
                         email: string
                         password: string |}>> =
        task {
            let findOne =
                find UsersColName {
                    filter {| email = email |}
                    projection {| email = 1; password = 1 |}
                    limit 1
                }

            try
                let! result =
                    database.Value.RunCommandAsync<FindResult<{| _id: ObjectId
                                                                 email: string
                                                                 password: string |}>>(
                        JsonCommand findOne
                    )

                return result.cursor.firstBatch |> Seq.tryHead
            with ex ->
                eprintfn $"TryFindByEmailWithPassword: [{ex.Message}]"
                return None
        }

    let Exists (email: string) : Task<bool> =
        task {
            let existsCmd =
                count {
                    collection UsersColName
                    query {| email = email |}
                }

            let! result = database.Value.RunCommandAsync<CountResult>(JsonCommand existsCmd)

            return result.n > 0
        }

    let TryCreate (user: SignupPayload) : Task<Option<User>> =
        task {
            let! result =
                let cmd =
                    insert UsersColName { documents [ user ] }

                database.Value.RunCommandAsync<InsertResult>(JsonCommand(cmd))

            if result.n > 0 && result.ok = 1.0 then
                return! TryFindByEmail user.email
            else
                return None
        }

    let CanLogin (user: LoginPayload) : Task<bool> =
        task {
            match! TryFindByEmailWithPassword user.email with
            | Some found -> return EnhancedVerify(user.password, found.password)
            | None -> return false
        }

    let TrySignup (user: SignupPayload) : Task<Result<User, SignupError>> =
        taskResult {
            do!
                Exists user.email
                |> TaskResult.requireFalse SignupError.AlreadyExists

            let pwregex =
                Regex("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9]).{8,}$")

            do!
                user.name
                |> Result.requireNotEmpty (SignupError.MissingField("name", "Name must not be empty"))

            do!
                user.email
                |> Result.requireNotEmpty (SignupError.MissingField("email", "Email must not be empty"))

            do!
                user.password
                |> pwregex.IsMatch
                |> Result.requireTrue (
                    SignupError.MissingField(
                        "password",
                        "The password must be at least 8 characters long and include a lower case and upper case letter"
                    )
                )

            let! user =
                TryCreate
                    { user with
                          password = EnhancedHashPassword user.password }
                |> TaskResult.requireSome SignupError.CouldNotCreate

            return user
        }
