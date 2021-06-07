module SpedySpa.Services

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
open SpeedySpa.Database
open System.Collections.Generic


[<Interface>]
type TemplateProvider =

    abstract member getTemplate<'T> : string * 'T option -> Task<string>

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
                database.Value.RunCommandAsync<InsertResult>(JsonCommand(insert UsersColName { documents [ user ] }))

            if result.n > 0 && result.ok = 1.0 then
                return! TryFindByEmail user.email
            else
                return None
        }
