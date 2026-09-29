namespace MiaouVSRG.Web.Server.API.Web.Users

open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Web.User.AboutMe
open NetCoreServer
open Percyqaz.Common
open Catnip

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


