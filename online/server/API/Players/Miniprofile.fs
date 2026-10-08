namespace MiaouVSRG.Web.Server.API.Players

open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Core.Score
open MiaouVSRG.Web.Shared.Requests.Players
open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Server.API

module Miniprofile =
    
    open Miniprofile

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let id, user = authorize headers

            let mean_accuracy = get_mean_accuracy_by_user_id id
            
            let get_lb_infos (keymode: int): int =
                let lb_combined =
                    match keymode with
                    | 4 -> Stats.leaderboard_4k_combined()
                    | 7 -> Stats.leaderboard_7k_combined()
                    | _ -> Stats.leaderboard_4k_combined()
                let mutable rank = 0
                for i in 0 .. lb_combined.Length - 1 do
                    let lb_entry_4k = lb_combined[i]
                    if lb_entry_4k.UserId = id then
                        rank <- i + 1
                            
                rank
            
            let overall_rating = user_global_rating_WIP id
            
            // TODO: Make a better ranking system, like the website's ranking system in /web/leaderboard
            let global_rank = get_lb_infos 4

            response.ReplyJson(
                {
                    MeanAccuracy = mean_accuracy
                    OverallRating = overall_rating
                    GlobalRank = global_rank
                    ProfilePictureLink = $"https://cdn.miaouvsrg.com/avatars/{id}.png"
                }
                : Response
            )
        }