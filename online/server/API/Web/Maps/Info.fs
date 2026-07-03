namespace Interlude.Web.Server.API.Web.Maps

open System.IO
open System.IO.Compression
open System.Linq
open Interlude.Web.Server.API
open Interlude.Web.Server.API.New.Charts
open Interlude.Web.Server.API.New.Charts.Add
open Interlude.Web.Server.Domain.New
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.Map.Info
open NetCoreServer
open Percyqaz.Common
open Prelude
open Prelude.Calculator
open Prelude.Charts
open Prelude.Data
open Prelude.Formats
open Prelude.Formats.Osu

module Info =
    let download_mapset_from_mino(url: string, output: string) =
         async{
            let zip_path = "mapset.osz"
            if Path.Exists(zip_path) then
                File.Delete(zip_path)
            if Path.Exists(output) then
                File.Delete(output)
            match! WebServices.download_file.RequestAsync((url, zip_path, ignore)) with
            | false ->
                Logging.Error "There was an error while downloading the map."
            | true ->
                ZipFile.ExtractToDirectory(zip_path, output)
                File.Delete(zip_path)
         }
        
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "chart"
            let diffs: ResizeArray<MapDifficulty> = ResizeArray<MapDifficulty>(Array.empty)
            let chart_id = query_params["chart"][0]
            let db_chart = Charts.get_chart_by_id chart_id
            if db_chart.IsNone then
                response.ReplyError(404, "chart not found")
            else
                let chart = db_chart.Value
                let mutable source_folder = ""
                let mutable require_folder_deletion = false
                let mutable background_url = ""
                if chart.DownloadLink.StartsWith("https://catboy.best") then
                    download_mapset_from_mino(chart.DownloadLink, chart.ChartId) |> Async.RunSynchronously
                    source_folder <- chart.ChartId
                    require_folder_deletion <- true
                else
                    source_folder <- chart.Path
                    background_url <- chart.ImageLink
                let files = Directory.GetFiles(source_folder, "*.osu")
                for file in files do
                    let beatmap =
                        match Beatmap.FromFile file with
                        | Ok b when b.General.Mode <> Gamemode.OSU_MANIA -> None
                        | Ok b -> Some b
                        | Error msg ->
                            Logging.Error "Parse error in osu! file %s: %O" file msg
                            None
                        
                    if beatmap.IsNone then
                        Logging.Debug $"The file {file} is not an osu!mania map"
                    else
                        if background_url = "" then
                            background_url <- $"https://catboy.best/preview/background/{beatmap.Value.Metadata.BeatmapSetID}?set=1"
                        
                        let action: ConversionAction =
                            {
                                Config = {
                                    AssetBehaviour = ConversionAssetBehaviour.CopyAssetFiles
                                    EtternaPackName = None
                                    ChangedAfter = None
                                    PackName = "osu!"
                                }
                                Source = file
                            }
                                
                        let converted_chart =
                            match Osu_To_Interlude.convert beatmap.Value action with
                            | Ok chart -> Some chart
                            | Error err ->
                                Logging.Error "%s ::: %s" (fst err) (snd err)
                                None
                                
                        let hash: string option =
                            match converted_chart with
                            | Some chart_import -> Some (Prelude.Charts.Chart.hash chart_import.Chart)
                            | None -> None
                                
                        let rating =
                            if converted_chart.IsSome then
                                Difficulty.calculate(1.0f<rate>, converted_chart.Value.Chart.Notes).Overall
                            else
                                0.0f
                                    
                        let length =
                            if converted_chart.IsSome then
                                format_duration (converted_chart.Value.Chart.LastNote - converted_chart.Value.Chart.FirstNote)
                            else
                                ""
                                    
                        let bpm =
                            if converted_chart.IsSome then
                                let mspb = Chart.find_most_common_bpm converted_chart.Value.Chart
                                let bpm = 60000.0f<ms/minute> / mspb |> float32
                                if System.Single.IsFinite(bpm) then
                                    bpm |> round |> int
                                else 0
                            else
                                0
                                    
                        let rice_count =
                            if converted_chart.IsSome then
                                let mutable count = 0
                                let rows = converted_chart.Value.Chart.Notes |> Array.map(_.Data)
                                for row in rows do
                                    for note in row do
                                        if note = NoteType.NORMAL then
                                            count <- count + 1
                                count
                            else
                                0
                            
                        let ln_count =
                            if converted_chart.IsSome then
                                let mutable count = 0
                                let rows = converted_chart.Value.Chart.Notes |> Array.map(_.Data)
                                for row in rows do
                                    for note in row do
                                        if note = NoteType.HOLDHEAD then
                                            count <- count + 1
                                count
                            else
                                0
                                
                        let diff: MapDifficulty = {
                            Hash = hash
                            Name = beatmap.Value.Metadata.Version
                            Artist = beatmap.Value.Metadata.Artist
                            Rating = rating
                            Length = length
                            BPM = bpm
                            RiceCount = rice_count
                            LNCount = ln_count
                            Mapper = beatmap.Value.Metadata.Creator
                            Keymode = converted_chart.Value.Chart.Keys
                        }
                            
                        diffs.Add(diff)
                
                if require_folder_deletion then
                    // Delete downloaded charts from Mino
                    Directory.Delete(source_folder, true)
                      
                let r: Response = {
                    Name = chart.Title
                    Difficulties = diffs.ToArray()
                    Ranked = chart.Ranked = 1
                    Background = background_url
                    DownloadLink = chart.DownloadLink
                    MiaoudirectLink = $"miaou://map/{chart.ChartId}"
                }
            
                response.ReplyJson(r)                        
            
        }

