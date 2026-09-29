namespace MiaouVSRG.CLI.Features.Backbeat

open System.IO
open Percyqaz.Common
open Percyqaz.Data
open Percyqaz.Data.Sqlite
open Catnip
open Catnip.Data.Library
open Catnip.Data.User
open MiaouVSRG.Web.Shared
open MiaouVSRG.CLI

[<AutoOpen>]
module Config =

    let BACKBEAT_SETTINGS_PATH = Path.Combine(Utils.MIAOUVSRG_PATH, "backbeat", "settings.json")

    let PACK_LIST_PATH = Path.Combine(Utils.MIAOUVSRG_PATH, "backbeat", "archive", "masterlist-sm")

    [<Json.AutoCodec>]
    type Config =
        {
            MiaouVSRGPath: string
            S3ApiKey: string
            S3ApiKeyID: string
        }
        static member Default =
            {
                MiaouVSRGPath = "C:/MiaouVSRG/dev"
                S3ApiKey = ""
                S3ApiKeyID = ""
            }

    let backbeat_config: Config =
        match JSON.FromFile BACKBEAT_SETTINGS_PATH with
        | Ok c -> c
        | Error e ->
            Logging.Error "Error loading settings (using default): %O" e
            Config.Default

    [<Json.AutoCodec>]
    type LoginCredentials = { Api: string; Token: string } with static member Default = { Api = "api.miaouvsrg.com"; Token = "" }

    do
        try
            JSON.ToFile (BACKBEAT_SETTINGS_PATH, true) backbeat_config
            let credentials : LoginCredentials = JSON.FromFile(Path.Combine(backbeat_config.MiaouVSRGPath, "Data", "login.json")) |> expect
            API.Client.init("https://" + credentials.Api)
            API.Client.authenticate(credentials.Token)
        with err -> printfn "%O" err; failwith "Error initialising backbeat utils"

    let miaouvsrg_chart_db =
        Directory.SetCurrentDirectory(backbeat_config.MiaouVSRGPath)
        let db_file = Path.Combine(get_game_folder "Songs", "charts.db")
        if not (File.Exists db_file) then failwith "Couldn't find your miaouvsrg charts.db"
        let db = Database.from_file db_file
        ChartDatabase.create true db

    let miaouvsrg_library: Library =
        {
            Charts = miaouvsrg_chart_db
            Collections = Unchecked.defaultof<_>
        }

    let miaouvsrg_scores_db =
        Directory.SetCurrentDirectory(backbeat_config.MiaouVSRGPath)
        let db_file = Path.Combine(get_game_folder "Data", "scores.db")
        if not (File.Exists db_file) then failwith "Couldn't find your miaouvsrg scores.db"
        let db = Database.from_file db_file
        UserDatabase.create false db

    let miaouvsrg_SKINS_PATH = Path.Combine(backbeat_config.MiaouVSRGPath, "Skins")