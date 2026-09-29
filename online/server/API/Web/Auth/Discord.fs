namespace MiaouVSRG.Web.Server.API.Web.Auth

open System
open System.Diagnostics
open MiaouVSRG.Web.Server
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Core.Stats
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Web.Leaderboard
open NetCoreServer
open Percyqaz.Common
open Catnip.Data.User.Stats

module Discord =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async{
            let host = require_host headers
            let state = Random().Next()
            let url =
                @"https://discord.com/api/oauth2/authorize?client_id="
                + SECRETS.DiscordClientId
                + "&redirect_uri=https%3A%2F%2F"
                + SECRETS.ApiBaseUrl
                + @"%2Fweb%2Flogin%2Fdiscord%2Ffinish&response_type=code&scope=identify&state="
                + $"{state}"
                
            let cookies = Array.create 1 ("discord_state", state.ToString(), None, host)
            response.ReplyRedirect(url, cookies)
        }
