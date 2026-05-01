namespace Interlude.Web.Server.API.New.Charts

open System.IO

open System.IO.Compression
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.New
open NetCoreServer
open Interlude.Web.Shared
open Percyqaz.Common
open Prelude
open Prelude.Calculator
open Prelude.Formats
open Prelude.Formats.Osu

module Download =
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "id"
            let chart_id = query_params["id"][0]
            let chart = Charts.get_chart_by_id chart_id
            if chart.IsSome then
                if chart.Value.Path = "" then
                    // 422: not processable
                    response.ReplyError(422, "Chart is not stored in the server")
                else
                    let output = $"{chart_id}.osz"
                    ZipFile.CreateFromDirectory(chart.Value.Path, $"./{output}")
                    let file = File.ReadAllBytes($"./{output}")
                    response.ReplyFile(file, output)
            else
                response.ReplyError(404, "chart not found")
        }

