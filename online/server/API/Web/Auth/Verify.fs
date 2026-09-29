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
open MiaouVSRG.Web.Shared.Requests.Web.Auth.Verify
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
