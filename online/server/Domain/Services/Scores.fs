namespace Interlude.Web.Server.Domain.Services

open Percyqaz.Common
open Prelude
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
            let is_ranked = rate >= 1.0f<rate> && mod_ranked_status = ModStatus.Ranked
            let ruleset = Backbeat.rulesets.[Score.PRIMARY_RULESET]

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
            keymode: int
        ) =
        async {

            match ModState.check mods with
            | Error message ->
                Logging.Error "Mod validation failed from user #%i: %s" user_id message
                return ScoreUploadOutcome.Failed
            | Ok ModStatus.Offline
            | Ok ModStatus.Unstored -> return ScoreUploadOutcome.Failed
            | Ok mod_ranked_status ->

            match Backbeat.Charts.fetch_new(chart_id) with
            | None ->
                let chart: New.Chart =
                    {
                        ChartId = chart_id
                        DownloadLink = if beatmapset_id.IsSome then sprintf "https://catboy.best/d/%in" beatmapset_id.Value else "not implemented"
                        Source = "osu!" //if beatmapset_id.IsSome then "osu!" else "none"
                        Keymode = keymode
                        Difficulty = "not implemented yet"
                        Title = "not implemented yet"
                        Ranked = 0
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