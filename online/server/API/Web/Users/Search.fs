namespace Interlude.Web.Server.API.Web.Users

open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.Search
open NetCoreServer
open Prelude.Data.User.Stats

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
                let res: Response = {
                    Username = db_user.Username
                    Country = "fr"
                    Followers = followers
                    Level = current_level stats.XP
                }
                
                response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "User not found !")
        }
