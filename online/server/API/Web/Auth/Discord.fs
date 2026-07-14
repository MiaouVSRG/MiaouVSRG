namespace Interlude.Web.Server.API.Web.Auth

open System
open Interlude.Web.Server
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Core.Stats
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.Leaderboard
open NetCoreServer
open Percyqaz.Common
open Prelude.Data.User.Stats

module Discord =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async{
            if not(headers.ContainsKey("X-Forwarded-Host")) then
                response.ReplyError(403, "You should not be here.")
            else    
                let state = Random().Next()
                let url =
                    @"https://discord.com/api/oauth2/authorize?client_id="
                    + SECRETS.DiscordClientId
                    + "&redirect_uri=https%3A%2F%2F"
                    + SECRETS.ApiBaseUrl
                    + @"%2Fweb%2Flogin%2Fdiscord%2Ffinish&response_type=code&scope=identify&state="
                    + $"{state}"
                    
                let cookies = Array.create 1 ("discord_state", state.ToString(), None, headers["X-Forwarded-Host"])
                response.ReplyRedirect(url, cookies)
        }
