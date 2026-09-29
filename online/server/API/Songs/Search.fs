namespace MiaouVSRG.Web.Server.API.Songs

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Backbeat

module Search =

    open Songs.Search

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let user_id, user = authorize headers

            require_query_parameter query_params "query"

            let query = query_params.["query"].[0]

            let results = Songs.search_songs 100 0 query

            response.ReplyJson(
                ({
                    Results = Array.map (fun (id, song) -> { SongId = id; Song = song }) results
                }
                : Response)
            )
        }