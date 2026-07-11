namespace Interlude.Web.Server.API.Web.Maps

open System.IO
open System.IO.Compression
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.New
open Interlude.Web.Server.Domain.Services
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.Map.Leaderboard
open NetCoreServer
open Percyqaz.Common
open Prelude.Calculator
open Prelude.Charts
open Prelude.Data
open Prelude.Formats
open Prelude.Formats.Osu
open Prelude.Gameplay.Replays
open Prelude.Gameplay.Rulesets.Defaults
open Prelude.Gameplay.Scoring
open Prelude.Mods

module Leaderboard =
    let download_mapset_from_mino(url: string, output: string) =
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
            require_query_parameter query_params "chart"
            let chart_id = query_params["chart"][0]
            match Interlude.Web.Server.Domain.New.Charts.get_chart_by_id chart_id with
            | Some chart ->
                
                let mutable scorable_chart = Unchecked.defaultof<Prelude.Charts.Chart>
                
                let mutable source_folder = ""
                let mutable require_folder_deletion = false
                if chart.DownloadLink.StartsWith("https://catboy.best") then
                    download_mapset_from_mino(chart.DownloadLink, chart.ChartId) |> Async.RunSynchronously
                    source_folder <- chart.ChartId
                    require_folder_deletion <- true
                else
                    source_folder <- chart.Path
                
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
                        Logging.Debug $"The file {file} is not an osu!mania map"
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
                            response.ReplyError(404, "chart not convertible")
                        else
                            let hash = Prelude.Charts.Chart.hash converted_chart.Value.Chart
                            if hash = chart_id then
                                scorable_chart <- converted_chart.Value.Chart
                
                let info = Scores.get_leaderboard_details chart_id

                let scores: Score array =
                    info
                    |> Array.map (fun (i, user, score, replay) ->
                        let replay_data = Replay.decompress_bytes replay.Data
                        let with_mods = ModState.apply score.Mods scorable_chart
                        let scoring =
                            ScoreProcessor.run
                                NORMAL
                                with_mods.Keys
                                (StoredReplay replay_data)
                                with_mods.Notes
                                score.Rate
                                
                        let default_rating = Difficulty.calculate(score.Rate, with_mods.Notes)
                        let user_rating = Performance.calculate default_rating scoring
                        
                        
                        {
                            Username = user.Username
                            Rank = i + 1
                            Rate = score.Rate
                            Mods = score.Mods
                            Timestamp = score.TimePlayed
                            Combo = scoring.BestCombo
                            MehCount = scoring.JudgementCounts[2]
                            PerfectCount = scoring.JudgementCounts[0]
                            MissCount = scoring.JudgementCounts[3]
                            GreatCount = scoring.JudgementCounts[1]
                            Acc = scoring.Accuracy
                            Rating = user_rating
                        }
                    )
                    
                if require_folder_deletion then
                    // Delete downloaded charts from Mino
                    Directory.Delete(source_folder, true)

                response.ReplyJson<Response>({ Scores = scores })

            | None ->
                response.ReplyError(404, "Chart not leaderboarded") |> ignore                      
            
        }

