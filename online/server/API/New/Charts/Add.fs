namespace Interlude.Web.Server.API.New.Charts

open System.IO

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

module Add =
    
    let format_duration(length: Time) =
        length
        |> fun x -> (x / 1000.0f / 60.0f |> int, (x / 1000f |> int) % 60)
        |> fun (x, y) -> sprintf "%im%02is" x y
    
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
                    require_query_parameter query_params "folder"
                    let filename = query_params["filename"][0]
                    let folder = query_params["folder"][0]
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
                                    PackName = "osu!"
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
                            
                        // We do this because '#' and '[' are considered as invalid chars when making the request
                        // e.g. the name '#FairyJoke' will be sent in the request as '!=-!FairyJoke'
                        // We are considering that no chart 
                        let realfilename = filename.Replace("!=-!", "#").Replace("?=-?", "[")
                        let realfolder = folder.Replace("!=-!", "#").Replace("?=-?", "[")
                            
                        // let downloadLink = $"./maps/BMS/{realfolder}/{realfilename}"
                        // let downloadLink = $"./maps/O2Jam/{cat}/{realfolder}/{realfilename}"
                        let downloadLink = $"./maps/osu/{realfolder}/{realfilename}"
                        let source = "osu!"
                        let title = beatmap.Metadata.Title
                        
                        let rating =
                            if chart.IsSome then
                                Difficulty.calculate(1.0f<rate>, chart.Value.Chart.Notes).Overall
                            else
                                0.0f
                                
                        let length =
                            if chart.IsSome then
                                format_duration (chart.Value.Chart.LastNote - chart.Value.Chart.FirstNote)
                            else
                                ""
                        
                        let rec find_background_file e =
                            match e with
                            | (Background(bg, _, _)) :: _ -> bg
                            | _ :: es -> find_background_file es
                            | [] -> ""
                            
                        let background_file = find_background_file beatmap.Events
                        
                        // OSU
                        let diffname = beatmap.Metadata.Version
                        
                        // O2JAM
                        // let o2_diff = beatmap.Metadata.Version.Split(" ")[2]
                        // let o2_lvl = o2_diff.Replace("[", "").Replace("]", "")
                        // let diffname = $"{cat} Lv.{o2_lvl}"
                        
                        // BMS
                        // let bms_diff_2 = beatmap.Metadata.Version.Split(" ")[1]
                        // let bms_raw_diff = bms_diff_2.Replace("[", "").Replace("]", "").Split("_")
                        // let diffname =
                        //     match bms_raw_diff[0] with
                        //     | "n" -> "Normal " + bms_raw_diff[1]
                        //     | "n2" -> "Normal 2 " + bms_raw_diff[1]
                        //     | "i" -> "Insane " + bms_raw_diff[1]
                        //     | "i2" -> "Insane 2 " + bms_raw_diff[1]
                        //     | "oj" -> "Overjoy " + bms_raw_diff[1]
                        //     | "o" -> "Overjoy " + bms_raw_diff[1]
                        //     | "sl" -> "Satellite " + bms_raw_diff[1]
                        //     | "st" -> "Stella " + bms_raw_diff[1]
                        //     | "sr" -> "Starlight " + bms_raw_diff[1]
                        //     | _ -> ""
                        
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
                                    Difficulty = rating
                                    Title = title
                                    Ranked = 1
                                    DifficultyName = diffname
                                    Length = length
                                    ImageLink = downloadLink.Replace(realfilename, background_file)
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
                    require_query_parameter query_params "diffname"
                    require_query_parameter query_params "length"
                    require_query_parameter query_params "image"
                    
                    
                    let chart: Chart = {
                        ChartId = (query_params["chartId"][0]).ToUpper()
                        DownloadLink = query_params["downloadLink"][0]
                        Source = query_params["source"][0]
                        Keymode = query_params["keymode"][0] |> int
                        Difficulty = query_params["difficulty"][0] |> float32
                        Title = query_params["title"][0]
                        Ranked = 0
                        DifficultyName = query_params["diffname"][0]
                        Length = query_params["length"][0]
                        ImageLink = query_params["image"][0]
                    }
                    
                    match Charts.get_chart_by_id chart.ChartId with
                    | Some _ ->
                        response.ReplyError(400, "Chart already exists !")
                    | None ->
                        let res = Charts.add chart
                        response.ReplyJson(res)
            else
                response.ReplyError(403, "You need to be a developper to use this endpoint c:")
        }

