namespace Interlude.Web.Server.API.Web.Users

open System
open System.Linq
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Core.Score
open Interlude.Web.Server.Domain.New
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.Completion
open NetCoreServer

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
            
            match User.by_username user_name with
            | Some (user_id, db_user) ->
                let mutable user_completion: (ChartInfo * UserScoreStat) array = Array.Empty()
                let mutable not_passed: ChartInfo array = Array.Empty()
                let charts = Charts.get_all |> Array.filter(_.IsRanked())
                let scores = Score.by_user_id user_id
                
                for chart in charts do
                    let mutable best_score: ScoreByUserIdModel = create_default_score_by_user_id
                    let mutable passed: bool = false
                    let chart_scores = scores |> Array.filter(fun s -> s.ChartId = chart.ChartId)
                    for score in chart_scores do
                        if score.Accuracy > best_score.Accuracy then
                            best_score <- score
                            passed <- true
                    
                    if passed then        
                        let score_stat = {
                            Accuracy = best_score.Accuracy
                        }
                        let chart_info: ChartInfo = {
                            ChartId = chart.ChartId
                            DownloadLink = chart.DownloadLink
                            Source = chart.Source
                            Keymode = chart.Keymode
                            Title = chart.Title
                            Difficulty = chart.Difficulty
                            Ranked = chart.Ranked
                        }
                        user_completion <- user_completion.Append ((chart_info, score_stat)) |> _.ToArray()
                    else
                        let chart_info: ChartInfo = {
                            ChartId = chart.ChartId
                            DownloadLink = chart.DownloadLink
                            Source = chart.Source
                            Keymode = chart.Keymode
                            Title = chart.Title
                            Difficulty = chart.Difficulty
                            Ranked = chart.Ranked
                        }
                        not_passed <- not_passed.Append(chart_info) |> _.ToArray()
                            
                            
                let res: Response = {
                    Passed = user_completion
                    Skipped = not_passed
                }
                
                response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "User not found !")
        }
