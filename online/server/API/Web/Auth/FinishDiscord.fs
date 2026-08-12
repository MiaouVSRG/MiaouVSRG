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
open Interlude.Web.Server.Online
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
        require_referer headers "https://discord.com/"
        
        async{
            let host = require_host headers
            let discord_state = require_cookie headers "discord_state"
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
            
            match User.by_discord_id(uint64 identity.id) with
            | Some (user_id, db_user) ->
                let is_online_in_game = Session.list_online_users() |> Array.contains((user_id, db_user.Username))
                let token =
                    // If the user is online ingame we dont generate a new token
                    // So that the user is not disconnected ingame and can still play online
                    if is_online_in_game then
                        db_user.AuthToken.Replace("+", "%2B")
                    else
                        let new_token = User.generate_auth_token ()
                        User.set_auth_token (user_id, new_token)
                        new_token.Replace("+", "%2B")
                        
                let cookies = Array.create 1 ("discord_state", "", Some 0, host)
                // TODO: Set a new variable in secrets.json that handles the base website URL
                response
                    .ReplyRedirect($"""https://{host.Replace("api.", "www.")}/user/login/validate?token={token}""", cookies)
            | None ->
                Logging.Info $"User {discord_tag} tried to connect via web, but does not have an account"
                response.ReplyRedirect("https://miaouvsrg.com/login_failed")
        }
