namespace Interlude.Web.Server.API.Web

open System
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Core.Stats
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.Leaderboard
open NetCoreServer
open Prelude.Data.User.Stats

module Leaderboard =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        
        async{
            let data = xp_leaderboard()
            // Array.map _.UserId replaces Array.map (fun x -> x.UserId)
            let users = data |> Array.map _.UserId |> User.by_ids |> Map.ofArray
            
            let result =
                data
                |> Array.map (fun lb_entry ->
                    let user = users[lb_entry.UserId]
                    {
                        Username = user.Username
                        Country = "fr"
                        Level = current_level lb_entry.XP
                        Playcount = lb_entry.Playtime
                        Accuracy = 0.0
                        Rating = 0
                    }
                )
            
            response.ReplyJson(result)
        }
