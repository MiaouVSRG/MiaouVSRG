namespace Interlude.Web.Server.API.Web.Users

open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.Login
open NetCoreServer
open Prelude
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