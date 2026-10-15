namespace MiaouVSRG.Web.Server.API.Players

open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Core.Score
open MiaouVSRG.Web.Shared.Requests.Players
open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Server.API

module ProfilePicture =
    
    open ProfilePicture

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let _ = authorize headers
            require_query_parameter query_params "username"
            let username = query_params["username"][0]
            
            let link =
                match User.by_username username with
                | Some (id, user) when user.ProfilePicture <> "" -> user.ProfilePicture
                | _ -> "https://cdn.miaouvsrg.com/avatars/empty.png"

            response.ReplyJson(
                {
                    ProfilePictureLink = $"{link}"
                }
                : Response
            )
        }