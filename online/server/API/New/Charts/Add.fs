namespace Interlude.Web.Server.API.New.Charts

open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.New
open NetCoreServer
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests
open Interlude.Web.Server.Domain.Services

module Add =
    open New.Charts.Add
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "chartId"
            require_query_parameter query_params "downloadLink"
            require_query_parameter query_params "source"
            
            
            let chart: Chart = {
                ChartId = (query_params["chartId"][0]).ToUpper()
                DownloadLink = query_params["downloadLink"][0]
                Source = query_params["source"][0]
            }
            
            match Charts.get_chart_by_id chart.ChartId with
            | Some _ ->
                response.ReplyError(400, "Chart already exists !")
            | None ->
                let res = Charts.add chart
                response.ReplyJson(res)
        }

