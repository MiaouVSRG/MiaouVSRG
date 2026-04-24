namespace Interlude.Web.Server.API.Web.Users

open System
open System.Linq
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.New
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.Search
open NetCoreServer
open Percyqaz.Common
open Prelude.Data.User.Stats
open Prelude.Gameplay.Rulesets
open Prelude.Gameplay.Scoring

module Search =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        
        async{
            require_query_parameter query_params "name"
            
            let user_name = query_params["name"][0]
            
            match User.by_username user_name with
            | Some (user_id, db_user) ->
                let followers = (Friends.get_followers_ids user_id).Count
                let stats = Stats.get_or_default user_id
                let scores = Score.by_user_id user_id
                let all_charts = Charts.get_all
                let osu_charts = Charts.get_by_source "osu!"
                let etterna_charts = Charts.get_by_source "Etterna"
                let o2Jam_charts = Charts.get_by_source "o2Jam"
                let bms_charts = Charts.get_by_source "BMS"
                
                let mutable total_acc = 0.0
                for score in scores do
                    total_acc <- score.Accuracy * 100.0
                let average_acc = total_acc / float scores.Length
                
                let get_completion(scores: Score.ScoreByUserIdModel array, charts: Chart array, keymode: int option): float32 =
                    let new_charts =
                        if keymode.IsSome then
                            charts |> Array.filter(fun chart -> chart.Keymode = keymode.Value)
                        else
                            charts
                            
                    let already_played: string array = Array.create scores.Length ""
                    let mutable played_maps = 0.0f
                    
                    for i in 0 .. scores.Length - 1 do
                        let score = scores[i]
                        
                        if not (already_played.Contains(score.ChartId)) && (new_charts |> Array.map(_.ChartId) |> Array.contains score.ChartId) then
                            already_played.SetValue(score.ChartId, i)
                            played_maps <- played_maps + 1.0f
                       
                    if played_maps > 0.0f then     
                        played_maps / float32 new_charts.Length
                    else
                        0.0f
                
                let get_user_grades (ruleset: Ruleset, scores: Score.ScoreByUserIdModel array): GradeCountInfo =
                    let already_played: (string * float32) array = Array.create scores.Length ("", 0.0f)
                    let mutable pass_scores = 0
                    let mutable clear_scores = 0
                    let mutable clearplus_scores = 0
                    let mutable overclear_scores = 0
                    let mutable overclearplus_scores = 0
                    let mutable perfect_scores = 0
                    for i in 0 .. scores.Length - 1 do
                        let score = scores[i]
                            
                        if not (already_played.Contains((score.ChartId, score.Rate))) then
                            already_played.SetValue((score.ChartId, score.Rate), i)
                            match ruleset.GradeName score.Grade with
                            | "PASS" -> pass_scores <- pass_scores + 1
                            | "CLEAR" -> clear_scores <- clear_scores + 1
                            | "CLEAR+" -> clearplus_scores <- clearplus_scores + 1
                            | "OVERCLEAR" -> overclear_scores <- overclear_scores + 1
                            | "OVERCLEAR+" -> overclearplus_scores <- overclearplus_scores + 1
                            | "PERFECT" -> perfect_scores <- perfect_scores + 1
                            | _ -> ignore 0
                    
                    {
                        Pass = pass_scores
                        Clear = clear_scores
                        ClearPlus = clearplus_scores
                        Overclear = overclear_scores
                        OverclearPlus = overclearplus_scores
                        Perfect = perfect_scores
                    }
                
                let get_lb_infos (keymode: int): int * float32 =
                    let lb_combined =
                        match keymode with
                        | 4 -> Stats.leaderboard_4k_combined()
                        | 7 -> Stats.leaderboard_7k_combined()
                        | _ -> Stats.leaderboard_4k_combined()
                    let mutable rank = 0
                    let mutable player_rating = 0.0f
                    for i in 0 .. lb_combined.Length - 1 do
                        let lb_entry_4k = lb_combined[i]
                        if lb_entry_4k.UserId = user_id then
                            rank <- i + 1
                            player_rating <- lb_entry_4k.Combined
                            
                    (rank, player_rating)
                    
                let rank_4k, rating_4k = get_lb_infos 4
                let rank_7k, rating_7k = get_lb_infos 7
                
                let easy_grades = get_user_grades(EASY, scores)
                let normal_grades = get_user_grades(NORMAL, scores)
                let hard_grades = get_user_grades(HARD, scores)
                let strict_grades = get_user_grades(STRICT, scores)
                
                let completion_percent_global = sprintf "%.2f%%" (get_completion(scores, all_charts, None) * 100.0f)
                let completion_percent_4k = sprintf "%.2f%%" (get_completion(scores, all_charts, Some 4) * 100.0f)
                let completion_percent_7k = sprintf "%.2f%%" (get_completion(scores, all_charts, Some 7) * 100.0f)
                
                let completion_osu = sprintf "%.2f%%" (get_completion(scores, osu_charts, None) * 100.0f)
                let completion_etterna = sprintf "%.2f%%" (get_completion(scores, etterna_charts, None) * 100.0f)
                let completion_o2jam = sprintf "%.2f%%" (get_completion(scores, o2Jam_charts, None) * 100.0f)
                let completion_bms = sprintf "%.2f%%" (get_completion(scores, bms_charts, None) * 100.0f)
                
                let profile_info: ProfileInfo = {
                    Username = db_user.Username
                    Country = "fr"
                    Followers = followers
                    Level = current_level stats.XP
                    StatsGlobal = {
                        GlobalRanking = rank_4k
                        CountryRanking = 0
                        PlayerRating = Math.Round(float (max rating_7k rating_4k), 2)
                        Completion = completion_percent_global
                    }
                    Stats4K = {
                        GlobalRanking = rank_4k
                        CountryRanking = 0
                        PlayerRating = Math.Round(float rating_4k, 2)
                        Completion = completion_percent_4k
                    }
                    Stats7K = {
                        GlobalRanking = rank_7k
                        CountryRanking = 0
                        PlayerRating = Math.Round(float rating_7k, 2)
                        Completion = completion_percent_7k
                    }
                    Playtime = format_long_time stats.Playtime
                    GradeCount = {
                        Easy = easy_grades
                        Normal = normal_grades
                        Hard = hard_grades
                        Strict = strict_grades
                    }
                    Avatar = "https://a.ppy.sh/25261784?1764839996.jpeg"
                    Banner = "https://assets.ppy.sh/user-profile-covers/25261784/4337e6766860ef2203e32c4c16b7f5c7c552a72d1522aadf2b1af10e726a21a1.jpeg"
                    Playcount = scores.Length
                    TotalHits = stats.NotesHit
                    OsuCompletion = completion_osu
                    EtternaCompletion = completion_etterna
                    O2JamCompletion = completion_o2jam
                    BMSCompletion = completion_bms
                    HitAccuracy = sprintf "%.2f%%" average_acc
                }
                
                let res: Response = {
                    ProfileInfo = profile_info
                }
                
                response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "User not found !")
        }
