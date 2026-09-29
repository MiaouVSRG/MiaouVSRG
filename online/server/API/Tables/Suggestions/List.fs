namespace MiaouVSRG.Web.Server.API.Tables.Suggestions

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Backbeat
open MiaouVSRG.Web.Server.Domain.Services

module List =

    open Tables.Suggestions.List

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

            let table_id = query_params.["table"].[0].ToLower()

            if not (Backbeat.Tables.exists table_id) then
                raise NotFoundException
            else

            let pending = TableSuggestion.pending_by_table table_id

            let user_votes_to_level_votes (source: Map<int64, int>) : Map<int, int> =
                source.Values
                |> Seq.groupBy id
                |> Seq.map (fun (level, votes) -> level, Seq.length votes)
                |> Map.ofSeq

            let suggestions =
                pending
                |> Array.map (fun p ->
                    {
                        ChartId = p.ChartId
                        Votes = user_votes_to_level_votes p.Votes
                        BackbeatInfo = Backbeat.Charts.by_hash p.ChartId
                    }
                )

            response.ReplyJson({ Suggestions = suggestions }: Response)
        }