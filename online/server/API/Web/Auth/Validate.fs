namespace MiaouVSRG.Web.Server.API.Web.Auth

open System
open System.Net.Http
open System.Net.Http.Json
open FParsec
open MiaouVSRG.Web.Server
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.API.Auth.Discord
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Core.Stats
open MiaouVSRG.Web.Server.Domain.Services
open MiaouVSRG.Web.Server.Domain.Services.Users
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Web.Auth.Validate
open NetCoreServer
open Percyqaz.Common

module Validate =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            
            let host = require_host headers

            if not(headers.ContainsKey("Origin")) then
                response.ReplyError(403, "You should not be here.")
            
            require_query_parameter query_params "token"
            
            let token = query_params["token"][0]
                
            match User.by_auth_token token with
            | Some _ ->
                let res: Response = {Success = true}
                let cookies = Array.create 1 ("token", token, None, host.Replace("api.", ""))
                response.ReplyJson(res, 200, cookies, headers["Origin"])
            | None ->
                response.ReplyError(404, "Invalid token")
        }
