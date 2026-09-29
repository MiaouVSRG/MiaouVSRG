namespace MiaouVSRG.Web.Server.API.Songs

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Backbeat

module Scan =

    open Songs.Scan

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let user_id, user = authorize headers

            if not (user.Badges.Contains Badge.DEVELOPER) then
                raise PermissionDeniedException

            require_query_parameter query_params "page"

            let ok, page = System.Int32.TryParse(query_params.["page"].[0], System.Globalization.CultureInfo.InvariantCulture)
            if not ok || page < 0 then
                raise (BadRequestException(Some "'page' must be a non-negative integer"))

            let results = Songs.scan_songs page

            response.ReplyJson(
                ({
                    Results = Array.map (fun (id, song) -> { SongId = id; Song = song }) results
                    HasNextPage = results.Length = 1000
                } : Response)
            )
        }