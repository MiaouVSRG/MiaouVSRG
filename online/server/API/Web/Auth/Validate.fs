namespace Interlude.Web.Server.API.Web.Auth

open System
open System.Net.Http
open System.Net.Http.Json
open FParsec
open Interlude.Web.Server
open Interlude.Web.Server.API
open Interlude.Web.Server.API.Auth.Discord
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.Core.Stats
open Interlude.Web.Server.Domain.Services
open Interlude.Web.Server.Domain.Services.Users
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.Auth.Validate
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
            
            for key in headers.Keys do
                Logging.Debug $"{key} : {headers[key]}"

            if not(headers.ContainsKey("Host")) then
                response.ReplyError(403, "You should not be here.")
            
            require_query_parameter query_params "token"
            
            let token = query_params["token"][0]
                
            match User.by_auth_token token with
            | Some _ ->
                let res: Response = {Success = true}
                let cookies = Array.create 1 ("token", token, None, headers["Host"].Replace("api.", ""))
                response.ReplyJson(res, 200, cookies, headers["Origin"])
            | None ->
                response.ReplyError(404, "Invalid token")
        }
