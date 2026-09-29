namespace MiaouVSRG.Web.Server.API.New.Charts

open System.Linq
open MiaouVSRG.Web.Server.Domain.New
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.New.Charts.Migrate
open NetCoreServer
open Percyqaz.Common
open Catnip.Formats.Osu

module Migrate =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let charts = Charts.get_all
            let mutable success = true
            
            let rec find_background_file e =
                match e with
                | (Background(bg, _, _)) :: _ -> bg
                | _ :: es -> find_background_file es
                | [] -> ""
            
            for chart in charts do
                Logging.Debug $"Migrating {chart.Title}..."
                
                let new_chart =
                    {chart with
                        DownloadLink = $"https://beta.api.miaouvsrg.com/v2/download?id={chart.ChartId}"
                    }
                
                Charts.update chart.ChartId new_chart |> ignore
            
            let res : Response = {
                Success = success
            }
            
            response.ReplyJson(res, if success then 400 else 500)
        }
