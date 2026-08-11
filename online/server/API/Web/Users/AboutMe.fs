namespace Interlude.Web.Server.API.Web.Users

open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.AboutMe
open NetCoreServer
open Percyqaz.Common
open Prelude

module AboutMe =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        
        async{
            let token = require_cookie headers "token"
            let id, _ = authorize_with_cookie token
            
            match JSON.FromString body with
            | Error _ ->
                raise (BadRequestException None)
            | Ok(request: Request) ->
                User.update_about_me(id, request.AboutMe)
                let res: Response = {Success = true}
                response.ReplyJson(res, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
        }


