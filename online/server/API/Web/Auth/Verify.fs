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
open Interlude.Web.Shared.Requests.Web.Auth.Verify
open NetCoreServer
open Percyqaz.Common

module Verify =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            if not(headers.ContainsKey("Origin")) then
                response.ReplyError(403, "You should not be here.")
            else
            
                let token = require_cookie headers "token"
                
                // If this function passes then a user is found
                let _ = authorize_with_cookie token
                
                let res: Response = {Success = true}
                response.ReplyJson(res, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
        }
