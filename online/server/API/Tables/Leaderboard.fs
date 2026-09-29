namespace MiaouVSRG.Web.Server.API.Tables

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Services

module Leaderboard =

    open Tables.Leaderboard

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "table"
            let _, _ = authorize headers

            let table_id = query_params.["table"].[0]

            if not (Backbeat.Tables.exists table_id) then
                raise NotFoundException
            else

            let info = Tables.get_leaderboard_details table_id

            let players: Player array =
                info
                |> Array.map (fun (i, user, rating) ->
                    {
                        Username = user.Username
                        Color = user.Color
                        Rank = i + 1
                        Rating = rating
                    }
                )

            response.ReplyJson({ Players = players }: Response)
        }