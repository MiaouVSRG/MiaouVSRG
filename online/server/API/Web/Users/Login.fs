namespace MiaouVSRG.Web.Server.API.Web.Users

open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Web.User.Login
open NetCoreServer
open Catnip
open BCrypt.Net

module Login =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            match JSON.FromString body with
            | Error _ -> raise (BadRequestException None)
            | Ok(request: Request) ->
                let dbuser = User.by_username(request.Username)
                if dbuser.IsNone then
                    response.ReplyError(404, $"There is no account for {request.Username}.")
                else
                    let user = snd dbuser.Value
                    if user.Password.IsNone then
                        response.ReplyError(401, "No password set for this account.")
                    else
                        let pwd = user.Password.Value
                        if BCrypt.Verify(request.Password, pwd) then
                            response.ReplyJson(true)
                        else
                            response.ReplyError(401, "Invalid credentials.")
        }