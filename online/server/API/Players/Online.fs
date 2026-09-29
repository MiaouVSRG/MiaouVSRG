namespace MiaouVSRG.Web.Server.API.Players

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Online
open MiaouVSRG.Web.Server.Domain.Core

module Online =

    open Players.Online

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let _, _ = authorize headers

            let online = Session.list_online_users ()
            let users = User.by_ids (online |> Array.map fst)

            response.ReplyJson(
                {
                    Players =
                        users
                        |> Array.map (fun (_, x) ->
                            {
                                Username = x.Username
                                Color = x.Color
                            }
                        )
                }
                : Response
            )
        }