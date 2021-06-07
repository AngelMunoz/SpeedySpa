namespace SpeedySpa.Types

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open System.Collections.Generic

[<BsonIgnoreExtraElements>]
type User =
    { _id: ObjectId
      name: string
      email: string }


type ObjectIdFilter = { ``$oid``: ObjectId }
type PlaceFilterById = { _id: ObjectIdFilter; owner: string }

type LoginPayload = { email: string; password: string }

type SignupPayload =
    { name: string
      email: string
      password: string }

type PaginatedResult<'T> = { count: int; items: seq<'T> }

[<Struct>]
type PaginationParams = { page: int; limit: int }

type AuthFormViewModel =
    { csrfToken: string
      errors: IDictionary<string, string> }
