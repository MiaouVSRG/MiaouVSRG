namespace MiaouVSRG.Web.Server.API.Tables

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Backbeat
open MiaouVSRG.Web.Server.Domain.Services

module List =

    open Tables.List

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let tables: Table array =
                Backbeat.Tables.TABLES
                |> Map.toArray
                |> Array.map (fun (id, info) ->
                    {
                        Id = id
                        Info = info
                        LastUpdated = TableLevel.get_time_last_changed id |> Option.defaultValue 0L
                    }
                )

            response.ReplyJson({ Tables = tables }: Response)
        }