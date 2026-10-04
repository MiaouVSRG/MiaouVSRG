namespace MiaouVSRG.Web.Server.API.Web.Users

open System
open System.Linq
open Catnip.Mods
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.New
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Web.User.TopPlays
open NetCoreServer
open Catnip.Gameplay.Rulesets

module RecentPlays =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        
        async{
            let mutable user = None
            
            if query_params.ContainsKey("name") then
                user <- User.by_username (query_params["name"][0])
            else
                let token = require_cookie headers "token"
                user <- User.by_auth_token token
            
            match user with
            | Some (user_id, _) ->
                let scores = Score.get_user_recent (user_id, 100)
                    
                let get_recent_plays (scores: Score.RecentScore array): Play array =
                    let mutable plays: Play array = Array.Empty()
                    
                    for score in scores do
                        let chartop = Charts.get_chart_by_id score.ChartId
                        if chartop.IsSome then
                            let chart = chartop.Value
                            let chart_background =
                                if chart.ImageLink.StartsWith("https://cdn.miaouvsrg.com/") then
                                    chart.ImageLink
                                elif chart.DownloadLink.Contains("https://catboy.best/") then
                                    $"""https://assets.ppy.sh/beatmaps/{chart.DownloadLink.Replace("https://catboy.best/d/", "").Replace("n", "")}/covers/cover@2x.jpg"""
                                else
                                    "not available"
                                    
                            let column_swapped = score.Mods.ContainsKey("column_swap")
                                        
                            let keymode =
                                if column_swapped then
                                    ColumnSwap.keys score.Mods["column_swap"]
                                else
                                    chart.Keymode
                                        
                            let column_swap_text = if column_swapped then $"{chart.Keymode}K to {keymode}K" else ""
                                    
                            let play: Play = {
                                ChartHash = score.ChartId
                                ChartName = chart.Title
                                ChartDiffName = chart.DifficultyName
                                ChartBackground = chart_background
                                ChartRating = chart.Difficulty
                                Keymode = keymode
                                Grade = NORMAL.GradeName score.Grade
                                Rate = float32 score.Rate
                                Accuracy = score.Accuracy
                                Rating = score.Rating
                                IsConvert = column_swapped
                                ConvertString = column_swap_text
                            }
                            plays <- plays.Append(play) |> _.ToArray()
                        
                    plays
                
                let recent_plays = get_recent_plays scores
                
                let res: Response = {
                    Plays = recent_plays
                }
                
                if not(query_params.ContainsKey("name")) then
                    response.ReplyJson(res, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
                else
                    response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "User not found !")
        }
