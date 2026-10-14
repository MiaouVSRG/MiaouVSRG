namespace MiaouVSRG.Web.Server.API.Charts.Scores

open System
open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Services
open MiaouVSRG.Web.Server.Domain

module UserRank =

    open Charts.Scores.UserRank

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "chart"
            let user_id, user = authorize headers

            let chart_id = query_params.["chart"].[0].ToUpper()

            match New.Charts.get_chart_by_id chart_id with
            | Some _ ->
                match Score.get_user_rank (chart_id, user_id) with
                | Some score ->
                    if score.ReplayId.IsSome then
                        let replay = (score.ReplayId.Value |> Replay.by_id)
                        if replay.IsSome then
                            let res: Response =
                                {
                                    Username = user.Username
                                    Rank = score.Position
                                    Replay = replay.Value.Data |> Convert.ToBase64String
                                    Rate = score.Rate
                                    Mods = score.Mods
                                    Timestamp = score.TimePlayed
                                }
                            response.ReplyJson<Response>(res)
                        else
                            response.ReplyError(404, "No replay for the score")
                | None -> response.ReplyError(404, "No score found")

            | None ->
                response.ReplyError(404, "Chart not leaderboarded") |> ignore
        }