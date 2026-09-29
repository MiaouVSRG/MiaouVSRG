namespace MiaouVSRG.Web.Server.API.Tables.Suggestions

open NetCoreServer
open Catnip
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Backbeat
open MiaouVSRG.Web.Server.Domain.Services

module Reject =

    open Tables.Suggestions.Reject

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let user_id, user = authorize headers

            if not (user.Badges.Contains Badge.TABLE_EDITOR) then
                raise PermissionDeniedException

            match JSON.FromString body with
            | Error e -> raise (BadRequestException None)
            | Ok(request: Request) ->

            if not (Backbeat.Tables.exists request.TableId) then
                raise NotFoundException

            let chart_id = request.ChartId.ToUpper()

            if TableSuggestion.reject request.TableId chart_id user_id request.Reason then
                response.ReplyJson(true)
            else
                response.ReplyJson(false)
        }