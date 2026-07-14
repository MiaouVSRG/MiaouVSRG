namespace Interlude.Web.Server.API.Web.Auth

open System
open System.Net.Http
open System.Net.Http.Json
open Interlude.Web.Server
open Interlude.Web.Server.API
open Interlude.Web.Server.API.Auth.Discord
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Core.Stats
open Interlude.Web.Server.Domain.Services
open Interlude.Web.Server.Domain.Services.Users
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.Leaderboard
open NetCoreServer
open Percyqaz.Common
open Prelude.Data.User.Stats

module Finish =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        
        require_query_parameter query_params "state"
        require_query_parameter query_params "code"
        require_cookie headers "discord_state"
        require_referer headers "https://discord.com/"
        
        async{
            
            if not(headers.ContainsKey("Host")) then
                response.ReplyError(403, "You should not be here.")
            else
                
                let discord_state = get_cookie headers "discord_state"
                let state = query_params["state"][0]
                let code = query_params["code"][0]
                
                if(discord_state <> state) then
                    response.ReplyError(401, "")
                
                // use code to get an oauth token on behalf of discord user
                let form =
                    dict
                        [
                            "client_id", SECRETS.DiscordClientId
                            "client_secret", SECRETS.DiscordClientSecret
                            "grant_type", "authorization_code"
                            "code", code
                            "redirect_uri", "https://" + SECRETS.ApiBaseUrl + "/web/login/discord/finish"
                        ]

                let data = new FormUrlEncodedContent(form)
                data.Headers.Clear()
                data.Headers.Add("Content-Type", "application/x-www-form-urlencoded")

                let! oauth_response =
                    http_client.PostAsync("https://discord.com/api/oauth2/token", data)
                    |> Async.AwaitTask

                if not oauth_response.IsSuccessStatusCode then
                    Logging.Error "Discord OAuth request failed: %s" oauth_response.ReasonPhrase

                    oauth_response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    |> Logging.Error "%s"

                    response.ReplyRedirect("https://miaouvsrg.com/login_failed")
                else

                let! oauth_data =
                    oauth_response.Content.ReadFromJsonAsync<DiscordOAuthResponse>()
                    |> Async.AwaitTask

                // use oauth token to get api information about "@me" on behalf of the user
                let identity_request =
                    new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me")

                identity_request.Headers.Clear()
                identity_request.Headers.Add("Authorization", oauth_data.token_type + " " + oauth_data.access_token)
                let identity_response = http_client.Send(identity_request)

                if not identity_response.IsSuccessStatusCode then
                    Logging.Error "Discord Identity request failed: %s" identity_response.ReasonPhrase

                    identity_response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    |> Logging.Error "%s"

                    response.ReplyRedirect("https://miaouvsrg.com/login_failed")
                else

                let! identity =
                    identity_response.Content.ReadFromJsonAsync<DiscordIdentityResponse>()
                    |> Async.AwaitTask

                let discord_tag =
                    if identity.discriminator <> "0" then
                        identity.username + "#" + identity.discriminator
                    else
                        identity.username

                // match Users.DiscordAuthFlow.receive_discord_callback (state, uint64 identity.id, discord_tag) with
                // | true -> response.ReplyRedirect("https://miaouvsrg.com/login_success")
                // | false -> response.ReplyRedirect("https://miaouvsrg.com/login_failed")
                
                match Auth.login_via_discord (uint64 identity.id) with
                | Error() ->
                    Logging.Info $"User {discord_tag} tried to connect via web, but does not have an account"
                    response.ReplyRedirect("https://miaouvsrg.com/login_failed")
                | Ok token ->
                    let format_token = token.Replace("+", "%2B")
                    let cookies = Array.create 1 ("discord_state", "", Some 0, headers["Host"])
                    
                    // TODO: Set a new variable in secrets.json that handles the base website URL
                    response
                        .ReplyRedirect($"https://miaouvsrg.com/validate?token={format_token}", cookies)
        }
