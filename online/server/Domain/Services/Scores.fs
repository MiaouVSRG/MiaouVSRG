namespace Interlude.Web.Server.Domain.Services

open System.IO
open System.IO.Compression
open Interlude.Web.Server.Domain.New
open Percyqaz.Common
open Prelude
open Prelude.Calculator
open Prelude.Data
open Prelude.Formats
open Prelude.Formats.Osu
open Prelude.Mods
open Prelude.Gameplay.Replays
open Prelude.Gameplay.Scoring
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Services
open Interlude.Web.Server.Domain
open Prelude.Gameplay.Rulesets

module Scores =

    [<RequireQualifiedAccess>]
    type ScoreUploadOutcome =
        | Failed
        | Unranked
        | Ranked of int option
        
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

    let new_leaderboard_position (score: Score) : int option =
        if not score.Ranked then None else

        let existing_lb = Score.get_leaderboard score.ChartId

        let mutable already_has_score = false
        let mutable position = Score.LEADERBOARD_SIZE
        let mutable i = 0

        while i < existing_lb.Length do
            if score.Accuracy > existing_lb.[i].Accuracy then
                position <- i
                i <- existing_lb.Length
            elif existing_lb.[i].UserId = score.UserId then
                already_has_score <- true
                i <- existing_lb.Length
            i <- i + 1

        if already_has_score then
            None
        elif position < Score.LEADERBOARD_SIZE then
            Some (position + 1)
        elif existing_lb.Length < Score.LEADERBOARD_SIZE then
            Some (existing_lb.Length + 1)
        else
            None
            
    let calculate_rating_and_accuracies(chart_id: string, replay: ReplayData, rate: Rate, mods: ModState) =
        // If we are at this stage it means that the chart is in the database for sure
        let db_chart = (Charts.get_chart_by_id chart_id).Value
        let source_folder = db_chart.ChartId
        download_mapset(db_chart.DownloadLink, db_chart.ChartId) |> Async.RunSynchronously
        
        let mutable final_rating: float32 = 0.0f
        let mutable final_accuracies: AccuraciesState = Unchecked.defaultof<AccuraciesState>
        
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
                else
                    let chart = converted_chart.Value.Chart
                    let hash = Prelude.Charts.Chart.hash converted_chart.Value.Chart
                    if hash <> chart_id then
                        ()
                    else
                        let with_mods = ModState.apply mods chart
                        let default_rating = Difficulty.calculate(rate, with_mods.Notes)
                        let scoring_easy =
                            ScoreProcessor.run
                                EASY
                                with_mods.Keys
                                (StoredReplay replay)
                                with_mods.Notes
                                rate
                        
                        let user_rating_easy = Performance.calculate default_rating scoring_easy
                                
                        let scoring_normal =
                            ScoreProcessor.run
                                NORMAL
                                with_mods.Keys
                                (StoredReplay replay)
                                with_mods.Notes
                                rate
                                
                        let scoring_hard =
                            ScoreProcessor.run
                                HARD
                                with_mods.Keys
                                (StoredReplay replay)
                                with_mods.Notes
                                rate
                                
                        let scoring_strict =
                            ScoreProcessor.run
                                STRICT
                                with_mods.Keys
                                (StoredReplay replay)
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
                        
                        final_accuracies <- accuracies
                        final_rating <- rating

        Directory.Delete(source_folder, true)
        (final_accuracies, final_rating)
            
    let proceed
        (
            user_id: int64,
            rate: Rate,
            mod_ranked_status: ModStatus,
            accuracy: float,
            chart_id: string,
            timestamp: int64,
            mods: ModState,
            judgement_counts: int array,
            combo_breaks: int,
            replay: ReplayData
        ): ScoreUploadOutcome =
            let is_ranked = rate >= 0.5f<rate> && mod_ranked_status = ModStatus.Ranked
            let ruleset = Backbeat.rulesets.[Score.PRIMARY_RULESET]
            
            let accuracies, rating = calculate_rating_and_accuracies (chart_id, replay, rate, mods)

            if accuracy >= 0.85 then // < 85% is counted as a fail in-game, we don't store failed scores
                let score: Score =
                    Score.create (
                        user_id,
                        chart_id,
                        timestamp,
                        rate,
                        mods,
                        is_ranked,
                        accuracy,
                        accuracies,
                        rating,
                        Grade.calculate ruleset.Grades accuracy,
                        Lamp.calculate ruleset.Lamps judgement_counts combo_breaks
                    )
            
                match new_leaderboard_position score with
                | Some p ->
                    let replay_id =
                        (user_id, chart_id, timestamp, replay)
                        |> Replay.create
                        |> Replay.save_leaderboard
                
                    let score_id = Score.save (score.WithReplay replay_id)
                    Logging.Info "Saved score %i with replay %i (#%i)" score_id replay_id p
                    ScoreUploadOutcome.Ranked (Some p)
                | None ->
                    Score.save score |> Logging.Info "Saved score %i, score not updated in leaderboard"
                    ScoreUploadOutcome.Ranked None
            
            else
            ScoreUploadOutcome.Ranked None

    let submit
        (
            user_id: int64,
            chart_id: string,
            replay: ReplayData,
            rate: Rate,
            mods: ModState,
            timestamp: int64,
            accuracy: float,
            judgement_counts: int array,
            combo_breaks: int,
            beatmapset_id: int option,
            keymode: int,
            source: string option,
            title: string,
            diff: float32,
            diffname: string,
            length: string,
            imagelink: string
        ) =
        async {

            match ModState.check mods with
            | Error message ->
                Logging.Error "Mod validation failed from user #%i: %s" user_id message
                return ScoreUploadOutcome.Failed
            | Ok ModStatus.Offline
            | Ok ModStatus.Unstored -> return ScoreUploadOutcome.Failed
            | Ok mod_ranked_status ->

            match New.Charts.get_chart_by_id chart_id with
            | None ->
                let chart: New.Chart =
                    {
                        ChartId = chart_id
                        DownloadLink = if beatmapset_id.IsSome then sprintf "https://catboy.best/d/%in" beatmapset_id.Value else "not available"
                        Source = if source.IsSome then source.Value else "none"
                        Keymode = keymode
                        Difficulty = diff
                        Title = title
                        Ranked = 0
                        DifficultyName = diffname
                        Length = length
                        ImageLink = imagelink
                        Path = ""
                    }
                        
                New.Charts.add (chart.FormatSource()) |> ignore
                return proceed (user_id, rate, mod_ranked_status, accuracy, chart_id, timestamp, mods, judgement_counts, combo_breaks, replay)
            | Some _ -> return proceed (user_id, rate, mod_ranked_status, accuracy, chart_id, timestamp, mods, judgement_counts, combo_breaks, replay)
        }

    let get_leaderboard_details (chart_id: string) =
        let leaderboard_scores = Score.get_leaderboard chart_id

        let users =
            leaderboard_scores
            |> Array.map (fun x -> x.UserId)
            |> User.by_ids
            |> Map.ofArray

        let replays =
            leaderboard_scores
            |> Array.choose (fun x -> x.ReplayId)
            |> Replay.by_ids
            |> Map.ofArray

        leaderboard_scores
        |> Array.indexed
        |> Array.choose (fun (i, score) ->
            match users.TryFind score.UserId with
            | None -> None
            | Some user ->

            match score.ReplayId with
            | None -> None
            | Some replay_id ->

            match replays.TryFind replay_id with
            | None -> None
            | Some replay ->

            Some(i, user, score, replay)
        )