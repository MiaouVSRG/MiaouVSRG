namespace Interlude.Web.Server.API.New.Charts

open System.IO

open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.New
open NetCoreServer
open Interlude.Web.Shared
open Percyqaz.Common
open Prelude.Formats
open Prelude.Formats.Osu

module Add =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let id, user = authorize headers
            if user.Badges.Contains(Badge.DEVELOPER) then
                if body <> "" then
                    require_query_parameter query_params "filename"
                    let filename = query_params["filename"][0]
                    use file = File.Create(filename)
                    let text_to_write = System.Text.Encoding.UTF8.GetBytes(body)
                    file.Write(text_to_write)
                    file.Close()
                    
                    let beatmap =
                        match Beatmap.FromFile filename with
                        | Ok b when b.General.Mode <> Gamemode.OSU_MANIA -> None
                        | Ok b -> Some b
                        | Error msg ->
                            Logging.Error "Parse error in osu! file %s: %O" filename msg
                            None
                            
                    match beatmap with
                    | None ->
                        response.ReplyError(500, "The beatmap could not be added to the db. Check that the map is an osu!mania map, or ask a dev.")
                    | Some beatmap ->
                        let action: ConversionAction =
                            {
                                Config = {
                                    AssetBehaviour = ConversionAssetBehaviour.CopyAssetFiles
                                    EtternaPackName = None
                                    ChangedAfter = None
                                    PackName = "BMS"
                                }
                                Source = filename
                            }
                            
                        let chart =
                            match Osu_To_Interlude.convert beatmap action with
                            | Ok chart -> Some chart
                            | Error err ->
                                Logging.Error "%s ::: %s" (fst err) (snd err)
                                None
                            
                        let hash: string option =
                            match chart with
                            | Some chart_import -> Some (Prelude.Charts.Chart.hash chart_import.Chart)
                            | None -> None
                            
                        let keymode: int option =
                            match chart with
                            | Some chart_import -> Some chart_import.Chart.Keys
                            | None -> None
                            
                        let downloadLink = $"./data/maps/BMS/{filename}"
                        // let downloadLink = $"./data/maps/O2Jam/{cat}/{filename}"
                        // let downloadLink = sprintf "https://catboy.best/d/%in" beatmap.Metadata.BeatmapSetID
                        let source = "BMS"
                        let title = beatmap.Metadata.Title

                        let bms_diff_2 = beatmap.Metadata.Version.Split(" ")[1]
                        let bms_raw_diff = bms_diff_2.Replace("[", "").Replace("]", "").Split("_")
                        let bms_diff =
                            match bms_raw_diff[0] with
                            | "n" -> "Normal " + bms_raw_diff[1]
                            | "n2" -> "Normal 2 " + bms_raw_diff[1]
                            | "i" -> "Insane " + bms_raw_diff[1]
                            | "i2" -> "Insane 2 " + bms_raw_diff[1]
                            | "oj" -> "Overjoy " + bms_raw_diff[1]
                            | "o" -> "Overjoy " + bms_raw_diff[1]
                            | "sl" -> "Satellite " + bms_raw_diff[1]
                            | "st" -> "Stella " + bms_raw_diff[1]
                            | "sr" -> "Starlight " + bms_raw_diff[1]
                            | _ -> ""
                        
                        // No need to keep the file in the server
                        File.Delete(filename)
                        
                        if chart.IsNone then
                            response.ReplyError(500, "Internal error. Chart is None but it should not.")
                        else
                            if Charts.get_chart_by_id(hash.Value).IsSome then
                                response.ReplyError(500, "Chart already in database :(")
                            else
                            
                                let db_chart: Chart = {
                                    ChartId = hash.Value
                                    DownloadLink = downloadLink
                                    Source = source
                                    Keymode = keymode.Value
                                    Difficulty = bms_diff
                                    Title = title
                                    Ranked = 0
                                }
                                
                                let res = Charts.add db_chart
                                response.ReplyJson(res)
                else            
                    require_query_parameter query_params "chartId"
                    require_query_parameter query_params "downloadLink"
                    require_query_parameter query_params "source"
                    require_query_parameter query_params "keymode"
                    require_query_parameter query_params "difficulty"
                    require_query_parameter query_params "title"
                    
                    
                    let chart: Chart = {
                        ChartId = (query_params["chartId"][0]).ToUpper()
                        DownloadLink = query_params["downloadLink"][0]
                        Source = query_params["source"][0]
                        Keymode = query_params["keymode"][0] |> int
                        Difficulty = query_params["difficulty"][0]
                        Title = query_params["title"][0]
                        Ranked = 0
                    }
                    
                    match Charts.get_chart_by_id chart.ChartId with
                    | Some _ ->
                        response.ReplyError(400, "Chart already exists !")
                    | None ->
                        let res = Charts.add chart
                        response.ReplyJson(res)
            else
                response.ReplyError(403, "You need to be a developper to use this endpoint.")
        }

