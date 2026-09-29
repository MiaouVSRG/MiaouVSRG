namespace MiaouVSRG.Web.Server.API.Web.Users

open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Web.User.Login
open NetCoreServer
open Catnip

module Register =
    
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
                if User.by_username(request.Username).IsSome then
                    response.ReplyError(409, $"Username {request.Username} is taken.")
                else
                    let user = User.create_with_password(request.Username, request.Password)
                    
                    User.save_new user |> ignore
                    response.ReplyJson(true)
        }