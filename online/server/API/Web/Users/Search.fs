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
            let cookies =
                headers
                |> Map.tryFind "Cookie"
                |> Option.map parseCookies
                |> Option.defaultValue Map.empty
                
            let mutable user = None
                
            if cookies.ContainsKey("token") then
                user <- User.by_auth_token cookies["token"]
            else
                require_query_parameter query_params "name"
                user <- User.by_username (query_params["name"][0])
            
            match user with
            | Some (user_id, db_user) ->
                let followers = (Friends.get_followers_ids user_id).Count
                let stats = Stats.get_or_default user_id
                let scores = Score.user_top_plays user_id
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
                        
                        let chart = Charts.get_chart_by_id(score.ChartId)
                        let ranked =
                            if chart.IsSome then
                                chart.Value.IsRanked()
                            else false
                            
                        if ranked && not (already_played.Contains((score.ChartId, score.Rate))) then
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
                    
                let get_top_plays (scores: Score.ScoreByUserIdModel array): Play array =
                    let mutable plays: Play array = Array.Empty()
                    let mutable charts: string array = Array.Empty()
                    
                    for score in scores do
                        let chartop = Charts.get_chart_by_id score.ChartId
                        if chartop.IsSome then
                            if not(charts.Contains(score.ChartId)) then
                                let chart = chartop.Value
                                let play: Play = {
                                    ChartHash = score.ChartId
                                    ChartName = chart.Title
                                    ChartDiffName = chart.DifficultyName
                                    ChartBackground = chart.ImageLink
                                    Keymode = chart.Keymode
                                    Grade = NORMAL.GradeName score.Grade
                                    Rate = score.Rate
                                    Accuracy = score.Accuracy
                                    Rating = score.Rating
                                }
                                plays <- plays.Append(play) |> _.ToArray()
                                charts <- charts.Append(score.ChartId) |> _.ToArray()
                        
                    plays
                    
                    
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
                
                let top_plays = get_top_plays scores
                
                // TODO: THIS IS TEMPORARY AND FOR INTERNAL TESTING ONLY
                // THIS IS NOT THE NEW RATING SYSTEM
                let mutable global_rating = 0.0f
                let scores_to_count =
                    if top_plays.Length >= 100 then
                        100
                    else
                        top_plays.Length - 1
                for i in 1..scores_to_count do
                    let play = top_plays[i]
                    global_rating <- global_rating + play.Rating
                
                global_rating <- global_rating / float32 scores_to_count
                
                let profile_info: ProfileInfo = {
                    Username = db_user.Username
                    Country = "fr"
                    Followers = followers
                    Level = current_level stats.XP
                    StatsGlobal = {
                        GlobalRanking = rank_4k
                        CountryRanking = 0
                        PlayerRating = float global_rating
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
                    Avatar = db_user.ProfilePicture
                    Banner = db_user.ProfileBanner
                    Playcount = scores.Length
                    TotalHits = stats.NotesHit
                    OsuCompletion = completion_osu
                    EtternaCompletion = completion_etterna
                    O2JamCompletion = completion_o2jam
                    BMSCompletion = completion_bms
                    HitAccuracy = sprintf "%.2f%%" average_acc
                    TopPlays = top_plays
                }
                
                let res: Response = {
                    ProfileInfo = profile_info
                }
                
                if cookies.ContainsKey("token") then
                    response.ReplyJson(res, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
                else
                    response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "User not found !")
        }
