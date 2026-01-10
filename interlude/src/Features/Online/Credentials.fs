namespace Interlude.Features.Online

open System.Diagnostics
open System.IO
open Percyqaz.Common
open Percyqaz.Data
open Prelude

[<Json.AutoCodec(false)>]
type Credentials =
    {
        DO_NOT_SHARE_THE_CONTENTS_OF_THIS_FILE_WITH_ANYONE_UNDER_ANY_CIRCUMSTANCES: string
        mutable Username: string
        mutable Token: string
        mutable Host: string
        mutable Api: string
        mutable Channel: string // Currently 'stable' or 'beta'
        mutable LastTimeUpdated: string // When was the last update client-side ?
    }
    static member Default =
        {
            DO_NOT_SHARE_THE_CONTENTS_OF_THIS_FILE_WITH_ANYONE_UNDER_ANY_CIRCUMSTANCES =
                "Doing so is equivalent to giving someone your account password"
            Username = ""
            Token = ""
            Host = "online.miaouvsrg.com"
            Api = "api.miaouvsrg.com"
            Channel = "stable"
            LastTimeUpdated = ""
        }

    static member Location : string = Path.Combine(get_game_folder "Data", "login.json")

    static member Load() =
        let path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)
        let saved_login_file = Path.Combine(path, "Data", "login.json.old")
        Logging.Debug "login file search : %s" saved_login_file
        if Path.Exists(saved_login_file) then
            Logging.Debug "There is already a login.json ! Copying it..."
            File.Move(saved_login_file, Path.Combine(path, "Data", "login.json"), true)
        if File.Exists Credentials.Location then
            File.SetAttributes(Credentials.Location, FileAttributes.Normal)

            Credentials.Location
            |> JSON.FromFile
            |> function
                | Ok res -> res
                | Error err ->
                    Logging.Error "Error loading login credentials, you will need to log in again.\n%O" err
                    Credentials.Default
        else
            Credentials.Default

    member this.Save() =
        JSON.ToFile (Credentials.Location, true) this