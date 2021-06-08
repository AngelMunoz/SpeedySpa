module SpeedySpa.Database

open System
open MongoDB.Driver
open MongoDB.Bson

let private url =
    Environment.GetEnvironmentVariable("SPEEDYSPA_DB_URL")
    |> Option.ofObj
    |> Option.defaultValue "mongodb://localhost:27017"

[<Literal>]
let private DbName = "speedyspa"

[<Literal>]
let UsersColName = "spe_users"

[<Literal>]
let PlacesColName = "spe_places"

let mongo = lazy (MongoClient(url))

let database = lazy (mongo.Value.GetDatabase(DbName))


type ObjectIdFilter = { ``$oid``: ObjectId }
type PlaceFilterById = { _id: ObjectIdFilter; owner: string }
