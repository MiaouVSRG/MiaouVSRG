namespace Interlude.Web.Server.API.Web.Users

open System
open System.IO
open System.IO.Compression
open System.Linq
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Core.Score
open Interlude.Web.Server.Domain.New
open Interlude.Web.Server.Domain.Services
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.Completion
open NetCoreServer
open Percyqaz.Common
open Prelude.Charts
open Prelude.Formats.Osu

module Completion =
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
            let limit =
                if not (query_params.ContainsKey "limit") then
                    None
                else
                    Some (int (query_params["limit"][0]))
            
            match User.by_username user_name with
            | Some (user_id, db_user) ->
                let mutable user_completion: CompletionCard array = Array.Empty()
                let charts = Charts.get_all_ranked
                let scores = Score.by_user_id user_id
                
                for chart in charts do
                    let mutable best_score: ScoreByUserIdModel = create_default_score_by_user_id
                    let mutable passed: bool = false
                    let chart_scores = scores |> Array.filter(fun s -> s.ChartId = chart.ChartId)
                    for score in chart_scores do
                        if score.Accuracy > best_score.Accuracy then
                            best_score <- score
                            passed <- true
                    
                    let chart_info: ChartInfo = {
                        ChartId = chart.ChartId
                        DownloadLink = chart.DownloadLink.Replace("./", "https://cdn.miaouvsrg.com/")
                        Source = chart.Source
                        Keymode = chart.Keymode
                        Title = chart.Title
                        Difficulty = chart.Difficulty
                        Ranked = chart.Ranked
                        DifficultyName = chart.DifficultyName
                        Length = chart.Length
                        Background = chart.ImageLink
                    }
                    if passed then
                        let ruleset = Backbeat.rulesets[Score.PRIMARY_RULESET]
                        let score_stat = {
                            Accuracy = best_score.Accuracy
                            Rate = best_score.Rate
                            Grade = ruleset.GradeName best_score.Grade
                        }
                        let card = {
                            Passed = passed
                            ChartInfo = chart_info
                            Score = Some score_stat
                        }
                        user_completion <- user_completion.Append card |> _.ToArray()
                    else
                        let card = {
                            Passed = passed
                            ChartInfo = chart_info
                            Score = None
                        }
                        user_completion <- user_completion.Append(card) |> _.ToArray()
                          
                if limit.IsSome then
                    // Try to send 50% of user passed maps and 50% of user not passed maps
                    let passed_scores = user_completion |> Array.filter(_.Passed)
                    let failed_scores = user_completion |> Array.filter(fun c -> not c.Passed)
                    let mutable filtered_passed_scores = Array.Empty()
                    let mutable filtered_failed_scores = Array.Empty()
                    let passed_scores_limit = limit.Value / 2
                    
                    if passed_scores.Length > passed_scores_limit then
                        for i in 0..passed_scores_limit - 1 do
                            filtered_passed_scores <- filtered_passed_scores.Append(passed_scores[i]) |> _.ToArray()
                    else
                        filtered_passed_scores <- passed_scores
                    
                    let failed_scores_limit =
                        if passed_scores.Length > passed_scores_limit then
                            limit.Value / 2
                        else
                            limit.Value - passed_scores.Length
                    
                    for i in 0..failed_scores_limit - 1 do
                        filtered_failed_scores <- filtered_failed_scores.Append(failed_scores[i]) |> _.ToArray()
                    
                    let res: Response = Array.concat [filtered_passed_scores;filtered_failed_scores]
                    response.ReplyJson(res)
                
                else    
                    let res: Response = user_completion
                    
                    response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "User not found !")
        }
