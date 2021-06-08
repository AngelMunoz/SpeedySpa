namespace SpeedySpa.Types

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

[<BsonIgnoreExtraElements>]
type User =
    { _id: ObjectId
      name: string
      email: string }

type LoginPayload = { email: string; password: string }

type SignupPayload =
    { name: string
      email: string
      password: string }

[<RequireQualifiedAccess>]
type RouteKind =
    | Login
    | Signup

[<RequireQualifiedAccess>]
type SignupError =
    | AlreadyExists
    | CouldNotCreate
    | MissingField of value: string * message: string


type PaginatedResult<'T> = { count: int; items: seq<'T> }

[<Struct>]
type PaginationParams = { page: int; limit: int }


type AuthFormViewModel =
    { csrfToken: string
      errors: Map<string, string> }

module ServiceTypes =
    open System.Threading.Tasks

    [<Interface>]
    type TemplateProvider =

        abstract member getTemplate<'T> : string * 'T option -> Task<string>
