namespace Interlude.Web.Server.API.New.Scores

open System.IO
open System.IO.Compression
open System.Linq
open System.Net.Http
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.New
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.New.Charts.Migrate
open NetCoreServer
open Percyqaz.Common
open Prelude
open Prelude.Calculator
open Prelude.Charts.Chart
open Prelude.Data
open Prelude.Formats
open Prelude.Formats.Osu
open Prelude.Gameplay.Replays
open Prelude.Gameplay.Rulesets
open Prelude.Gameplay.Scoring
open Prelude.Mods

module Migrate =
    
    let download_mapset(url: string, output: string) =
         async{
            let zip_path = "mapset.osz"
            if Path.Exists(zip_path) then
                File.Delete(zip_path)
            if Path.Exists(output) then
                File.Delete(output)
            match! WebServices.download_file.RequestAsync((url, zip_path, ignore)) with
            | false ->
                Logging.Error "There was an error while downloading the map."
            | true ->
                ZipFile.ExtractToDirectory(zip_path, output)
                File.Delete(zip_path)
         }
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let scores = Score.get_all
            let mutable success = true
            let mutable ok_scores = 0
            let mutable default_scores = 0
            let mutable score_ids_default: int64 array = Array.empty
            
            let from = int(query_params["from"][0])
            
            for score in scores do
                if score.Id < from then
                    Logging.Debug $"Skipping score #{score.Id}"
                    ()
                else    
                    Logging.Debug $"Migrating score #{score.Id}..."
                    
                    let mutable has_to_default = true
                    
                    let chart_option = Charts.get_chart_by_id score.ChartId
                    
                    if chart_option.IsNone then
                        Logging.Error "no chart for this score ?"
                        success <- false
                    else
                        let chart_db = chart_option.Value
                        let mutable source_folder = chart_db.ChartId
                        let mutable require_folder_deletion = true
                        
                        if chart_db.DownloadLink = "not available" then
                            let httpclient = new System.Net.Http.HttpClient()
                            let! response = httpclient.GetAsync("https://cdn.yavsrg.net/" + chart_db.ChartId) |> Async.AwaitTask

                            if not response.IsSuccessStatusCode then
                                Logging.Debug "Chart notes not found on server"
                            else

                                use! stream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                                use br = new BinaryReader(stream)

                                match read_headless chart_db.Keymode br with
                                | Error reason ->
                                    Logging.Debug "Malformed chart: %s" reason
                                | Ok chart_data ->
                                    let hash = Prelude.Charts.Chart.hash chart_data
                                    if hash <> chart_db.ChartId then
                                        ()
                                    else
                                        if score.ReplayId.IsNone then
                                            Logging.Error "score does not contains replay"
                                        else
                                            let replay = Replay.by_id score.ReplayId.Value
                                            if replay.IsNone then
                                                Logging.Error "score does contains replay id, but no replay was found for this id"
                                            else
                                                let replay_data = Replay.decompress_bytes replay.Value.Data
                                                let with_mods = ModState.apply score.Mods chart_data
                                                let rate: Rate = LanguagePrimitives.Float32WithMeasure<rate> score.Rate
                                                let default_rating = Difficulty.calculate(rate, with_mods.Notes)
                                                let scoring_easy =
                                                    ScoreProcessor.run
                                                        EASY
                                                        with_mods.Keys
                                                        (StoredReplay replay_data)
                                                        with_mods.Notes
                                                        rate
                                                            
                                                let user_rating_easy = Performance.calculate default_rating scoring_easy
                                                            
                                                let scoring_normal =
                                                    ScoreProcessor.run
                                                        NORMAL
                                                        with_mods.Keys
                                                        (StoredReplay replay_data)
                                                        with_mods.Notes
                                                        rate
                                                            
                                                let scoring_hard =
                                                    ScoreProcessor.run
                                                        HARD
                                                        with_mods.Keys
                                                        (StoredReplay replay_data)
                                                        with_mods.Notes
                                                        rate
                                                    
                                                let scoring_strict =
                                                    ScoreProcessor.run
                                                        STRICT
                                                        with_mods.Keys
                                                        (StoredReplay replay_data)
                                                        with_mods.Notes
                                                        rate
                                                            
                                                let rating = user_rating_easy
                                                    
                                                let accuracies_init: AccuraciesState = Map.empty
                                                let accuracies: AccuraciesState =
                                                    accuracies_init
                                                    |> Map.add "EASY" scoring_easy.Accuracy
                                                    |> Map.add "NORMAL" scoring_normal.Accuracy
                                                    |> Map.add "HARD" scoring_hard.Accuracy
                                                    |> Map.add "STRICT" scoring_strict.Accuracy
                                                        
                                                let new_score: Score =
                                                    {
                                                        UserId = score.UserId
                                                        ChartId = score.ChartId
                                                        TimePlayed = score.TimePlayed
                                                        TimeUploaded = score.TimeUploaded
                                                        Rate = rate
                                                        Mods = score.Mods
                                                        Ranked = true
                                                        Accuracy = score.Accuracy
                                                        Accuracies = accuracies
                                                        Rating = rating
                                                        Grade = score.Grade
                                                        Lamp = score.Lamp
                                                        ReplayId = score.ReplayId
                                                    }
                                                        
                                                let result = Score.migrate_accuracies_and_ratings (score.Id, new_score)
                                                if not result then
                                                    Logging.Error "n'a pas réussi à update le score"
                                                    success <- false
                                                else
                                                    Logging.Debug "OK"
                                                    has_to_default <- false
                                                    ok_scores <- ok_scores + 1
                                    
                        
                        if chart_db.Title <> "Shin'en no Mermaid (w/ YUC'e)" && chart_db.ChartId <> "833872DEA7AF49A4397947BB21892F62281490837D916F800366275519FD21A3" && chart_db.ChartId <> "B0DCF4ACF391DCF280F1A8F90F662C9CC73CF3D136E73648B46256AA6737144C" then
                        
                            download_mapset(chart_db.DownloadLink, chart_db.ChartId) |> Async.RunSynchronously
                            
                            let files = Directory.GetFiles(source_folder, "*.osu")
                            for file in files do
                                let beatmap =
                                    match Beatmap.FromFile file with
                                    | Ok b when b.General.Mode <> Gamemode.OSU_MANIA -> None
                                    | Ok b -> Some b
                                    | Error msg ->
                                        Logging.Error "Parse error in osu! file %s: %O" file msg
                                        None
                                    
                                if beatmap.IsNone then
                                    Logging.Error $"The file {file} is not an osu!mania map"
                                    success <- false
                                else
                                    let action: ConversionAction =
                                        {
                                            Config = {
                                                AssetBehaviour = ConversionAssetBehaviour.CopyAssetFiles
                                                EtternaPackName = None
                                                ChangedAfter = None
                                                PackName = "osu!"
                                            }
                                            Source = file
                                        }
                                            
                                    let converted_chart =
                                        match Osu_To_Interlude.convert beatmap.Value action with
                                        | Ok chart -> Some chart
                                        | Error err ->
                                            Logging.Error "%s ::: %s" (fst err) (snd err)
                                            None
                                            
                                    if converted_chart.IsNone then
                                        Logging.Error "chart not convertible"
                                        success <- false
                                    else
                                        let chart = converted_chart.Value.Chart
                                        let hash = Prelude.Charts.Chart.hash converted_chart.Value.Chart
                                        if hash <> chart_db.ChartId then
                                            ()
                                        else
                                            if score.ReplayId.IsNone then
                                                Logging.Error "score does not contains replay"
                                            else
                                                let replay = Replay.by_id score.ReplayId.Value
                                                if replay.IsNone then
                                                    Logging.Error "score does contains replay id, but no replay was found for this id"
                                                else
                                                    let replay_data = Replay.decompress_bytes replay.Value.Data
                                                    let with_mods = ModState.apply score.Mods chart
                                                    let rate: Rate = LanguagePrimitives.Float32WithMeasure<rate> score.Rate
                                                    let default_rating = Difficulty.calculate(rate, with_mods.Notes)
                                                    let scoring_easy =
                                                        ScoreProcessor.run
                                                            EASY
                                                            with_mods.Keys
                                                            (StoredReplay replay_data)
                                                            with_mods.Notes
                                                            rate
                                                            
                                                    let user_rating_easy = Performance.calculate default_rating scoring_easy
                                                            
                                                    let scoring_normal =
                                                        ScoreProcessor.run
                                                            NORMAL
                                                            with_mods.Keys
                                                            (StoredReplay replay_data)
                                                            with_mods.Notes
                                                            rate
                                                            
                                                    let scoring_hard =
                                                        ScoreProcessor.run
                                                            HARD
                                                            with_mods.Keys
                                                            (StoredReplay replay_data)
                                                            with_mods.Notes
                                                            rate
                                                    
                                                    let scoring_strict =
                                                        ScoreProcessor.run
                                                            STRICT
                                                            with_mods.Keys
                                                            (StoredReplay replay_data)
                                                            with_mods.Notes
                                                            rate
                                                            
                                                    let rating = user_rating_easy
                                                    
                                                    let accuracies_init: AccuraciesState = Map.empty
                                                    let accuracies: AccuraciesState =
                                                        accuracies_init
                                                        |> Map.add "EASY" scoring_easy.Accuracy
                                                        |> Map.add "NORMAL" scoring_normal.Accuracy
                                                        |> Map.add "HARD" scoring_hard.Accuracy
                                                        |> Map.add "STRICT" scoring_strict.Accuracy
                                                        
                                                    let new_score: Score =
                                                        {
                                                            UserId = score.UserId
                                                            ChartId = score.ChartId
                                                            TimePlayed = score.TimePlayed
                                                            TimeUploaded = score.TimeUploaded
                                                            Rate = rate
                                                            Mods = score.Mods
                                                            Ranked = true
                                                            Accuracy = score.Accuracy
                                                            Accuracies = accuracies
                                                            Rating = rating
                                                            Grade = score.Grade
                                                            Lamp = score.Lamp
                                                            ReplayId = score.ReplayId
                                                        }
                                                        
                                                    let result = Score.migrate_accuracies_and_ratings (score.Id, new_score)
                                                    if not result then
                                                        Logging.Error "n'a pas réussi à update le score"
                                                        success <- false
                                                    else
                                                        Logging.Debug "OK"
                                                        has_to_default <- false
                                                        ok_scores <- ok_scores + 1
                                                    
                            if require_folder_deletion then
                                // Delete downloaded charts
                                Directory.Delete(source_folder, true)
                    
                    if has_to_default then
                        let rate: Rate = LanguagePrimitives.Float32WithMeasure<rate> score.Rate
                        let new_score: Score =
                            {
                                UserId = score.UserId
                                ChartId = score.ChartId
                                TimePlayed = score.TimePlayed
                                TimeUploaded = score.TimeUploaded
                                Rate = rate
                                Mods = score.Mods
                                Ranked = true
                                Accuracy = score.Accuracy
                                Accuracies = Map.empty
                                Rating = 0.0f
                                Grade = score.Grade
                                Lamp = score.Lamp
                                ReplayId = score.ReplayId
                            }
                            
                        let result = Score.migrate_accuracies_and_ratings (score.Id, new_score)
                        if not result then
                            Logging.Error "n'a pas réussi à update le score en default"
                            success <- false
                        else
                            Logging.Debug "OK"
                            
                            default_scores <- default_scores + 1
                            score_ids_default <- score_ids_default.Append(score.Id) |> _.ToArray()
            
            let res : Response = {
                Success = success
            }
            
            Logging.Debug "SUMMARY :"
            Logging.Debug $"OK scores : ${ok_scores} / ${scores.Length}"
            Logging.Debug $"Defaulted scores : ${default_scores} / ${scores.Length}"
            Logging.Debug "List of all scores id that were defaulted :"
            for d_score in score_ids_default do
                Logging.Debug $"{d_score}"
            
            response.ReplyJson(res, if success then 400 else 500)
        }
