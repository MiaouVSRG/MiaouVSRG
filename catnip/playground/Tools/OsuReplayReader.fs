module OsuReplayReader

open System
open System.IO
open Percyqaz.Common
open Catnip
open Catnip.Charts
open Catnip.Formats
open Catnip.Formats.Osu
open Catnip.Data.OsuClientInterop
open Catnip.Data.Library.Imports
open Catnip.Gameplay.Replays
open Catnip.Gameplay.Rulesets
open Catnip.Gameplay.Scoring

let private compare_miaouvsrg_implementation_to_osu
    (
        chart: Chart,
        header: ChartImportHeader,
        replay: ReplayData,
        rate: Rate,
        chart_od: float,
        score_data: OsuScoreDatabase_Score
    ) =

    let window_modifier =
        if score_data.ModsUsed &&& Mods.Easy <> Mods.None then
            OsuMania.Easy
        elif score_data.ModsUsed &&& Mods.HardRock <> Mods.None then
            OsuMania.HardRock
        else
            OsuMania.NoMod

    let score_v2 = score_data.ModsUsed &&& Mods.ScoreV2 <> Mods.None

    let metric =
        ScoreProcessor.run
            (OsuMania.create (float32 chart_od) window_modifier)
            chart.Keys
            (StoredReplay(replay))
            chart.Notes
            rate

    let osu_accuracy =
        let g_300 = if score_v2 then 305.0 else 300.0

        let sum =
            g_300
            * float (
                score_data.CountGeki
                + score_data.Count300
                + score_data.CountKatu
                + score_data.Count100
                + score_data.Count50
                + score_data.CountMiss
            )

        g_300 * float (score_data.CountGeki)
        + 300.0 * float (score_data.Count300)
        + 200.0 * float score_data.CountKatu
        + 100.0 * float score_data.Count100
        + 50.0 * float score_data.Count50
        |> fun total -> total / sum * 100.0

    let osu_judgements =
        (int score_data.CountGeki,
         int score_data.Count300,
         int score_data.CountKatu,
         int score_data.Count100,
         int score_data.Count50,
         int score_data.CountMiss)

    let miaouvsrg_accuracy = metric.Accuracy * 100.0

    let miaouvsrg_judgements =
        (metric.JudgementCounts.[0],
         metric.JudgementCounts.[1],
         metric.JudgementCounts.[2],
         metric.JudgementCounts.[3],
         metric.JudgementCounts.[4],
         metric.JudgementCounts.[5])

    printfn
        "\nScore on %s [%s] -- od%.1f%s +%A"
        header.Title
        header.DiffName
        chart_od
        (if score_v2 then "V2" else "")
        window_modifier

    printfn " miaouvsrg: %.2f%%\tosu!: %.2f%%" miaouvsrg_accuracy osu_accuracy

    if score_v2 then
        printfn " Skipping because scorev2 isn't supported yet ..."
    elif osu_judgements <> miaouvsrg_judgements then
        printfn " !! Not an exact match of judgements !!"
        printfn " miaouvsrg: %A\tosu!: %A" miaouvsrg_judgements osu_judgements
        Console.ReadLine() |> ignore
    else
        printfn " Exact match, well done"

let read_scores () =

    Logging.Info "Reading osu database ..."

    use file = Path.Combine(OSU_SONG_FOLDER, "..", "scores.db") |> File.OpenRead

    use reader = new BinaryReader(file, Text.Encoding.UTF8)
    let scores = OsuScoreDatabase.Read(reader)

    use file = Path.Combine(OSU_SONG_FOLDER, "..", "osu!.db") |> File.OpenRead

    use reader = new BinaryReader(file, Text.Encoding.UTF8)
    let main_db = OsuDatabase.Read(reader)

    for beatmap_data in main_db.Beatmaps |> Seq.filter (fun b -> b.Mode = 3uy) do
        let osu_file =
            Path.Combine(OSU_SONG_FOLDER, beatmap_data.FolderName, beatmap_data.Filename)

        let chart, od =
            try
                let beatmap = Beatmap.FromFile osu_file |> expect

                Osu_To_Miaouvsrg.convert
                    beatmap
                    {
                        Config = ConversionOptions.Pack("osu!", None, LinkAssetFiles)
                        Source = osu_file
                    }
                |> expect
                |> Some,
                beatmap.Difficulty.OverallDifficulty
            with _ ->
                None, 8.0

        if chart.IsNone then
            ()
        else

            match scores.Beatmaps |> Seq.tryFind (fun b -> b.Hash = beatmap_data.Hash) with
            | Some scores ->
                for score in scores.Scores do
                    let replay_file =
                        Path.Combine(
                            OSU_SONG_FOLDER,
                            "..",
                            "Data",
                            "r",
                            sprintf "%s-%i.osr" score.BeatmapHash score.Timestamp
                        )

                    let replay_data = OsuReplay.TryReadFile(replay_file).Value

                    let miaouvsrg_replay =
                        OsuReplay.decode (replay_data, chart.Value.Chart.FirstNote, 1.0f<rate>)

                    match Mods.to_miaouvsrg_rate_and_mods replay_data.ModsUsed with
                    | Some(rate, _) ->
                        compare_miaouvsrg_implementation_to_osu (
                            chart.Value.Chart,
                            chart.Value.Header,
                            miaouvsrg_replay,
                            rate,
                            od,
                            replay_data
                        )
                    | None -> ()

            | None -> ()

    printfn "All scores processed!"
    Console.ReadLine() |> ignore