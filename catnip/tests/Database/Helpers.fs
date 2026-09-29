namespace Catnip.Tests.Database

open System
open NUnit.Framework
open Percyqaz.Data.Sqlite
open Catnip.Data.User
open Catnip.Data.Library

[<SetUpFixture>]
type Setup() =

    let mutable conn: IDisposable = Unchecked.defaultof<_>

    [<OneTimeSetUp>]
    member _.Setup() =
        let db, _conn = Database.in_memory "miaouvsrg"

        // migrate database (same database can have the tables that are normally split over 2 databases)
        UserDatabase.create false db |> ignore
        ChartDatabase.create false db |> ignore
        conn <- _conn // in-memory database persists until teardown where it gets disposed

    [<OneTimeTearDown>]
    member _.Teardown() = conn.Dispose()

[<AutoOpen>]
module Helpers =

    let in_memory () =
        Database.in_memory "miaouvsrg"
