namespace Interlude.Web.Server.Domain.New

open Discord
open Percyqaz.Data
open Percyqaz.Data.Sqlite
open Percyqaz.Common

open Interlude.Web.Server

type Chart =
    {
        ChartId: string
        DownloadLink: string
        Source: string
        Keymode: int
        Title: string
        Difficulty: float32 // Chart rating
        Ranked: int
        DifficultyName: string
        Length: string // Will format this as string so no conversion is really needed
        ImageLink: string
    }
    member this.FormatSource() = {
        this with Source = if this.Source.Equals("osu!") || this.Source.Equals("Etterna") || this.Source.Equals("BMS") || this.Source.Equals("o2jam") then this.Source else "none"
    }
    
    member this.IsRanked() = this.Ranked = 1

module Charts =
    let internal CREATE_TABLE: NonQuery<unit> =
        { NonQuery.without_parameters () with
            SQL =
                """
            CREATE TABLE charts (
                Id TEXT PRIMARY KEY NOT NULL,
                DownloadLink TEXT NOT NULL,
                Source TEXT NOT NULL,
                Keymode INTEGER NOT NULL,
                Difficulty REAL NOT NULL,
                Title TEXT NOT NULL,
                Ranked INTEGER NOT NULL,
                DifficultyName TEXT NOT NULL,
                Length TEXT NOT NULL,
                ImageLink TEXT NOT NULL
            );
            """
        }
        
    let private ADD: Query<Chart, string> =
        {
            SQL =
                """
            INSERT INTO charts (Id, DownloadLink, Source, Keymode, Difficulty, Title, Ranked, DifficultyName, Length, ImageLink)
            VALUES (@ChartId, @DownloadLink, @Source, @Keymode, @Difficulty, @Title, @Ranked, @DifficultyName, @Length, @ImageLink)
            RETURNING Id;
            """
            Parameters =
                [
                    "@ChartId", SqliteType.Text, -1
                    "@DownloadLink", SqliteType.Text, -1
                    "@Source", SqliteType.Text, -1
                    "@Keymode", SqliteType.Integer, 8
                    "@Difficulty", SqliteType.Real, -1
                    "@Title", SqliteType.Text, -1
                    "@Ranked", SqliteType.Integer, 8
                    "@DifficultyName", SqliteType.Text, -1
                    "@Length", SqliteType.Text, -1
                    "@ImageLink", SqliteType.Text, -1
                ]
            FillParameters =
                (fun p chart ->
                    p.String chart.ChartId
                    p.String chart.DownloadLink
                    p.String chart.Source
                    p.Int64 chart.Keymode
                    p.Float32 chart.Difficulty
                    p.String chart.Title
                    p.Int32 chart.Ranked
                    p.String chart.DifficultyName
                    p.String chart.Length
                    p.String chart.ImageLink
                )
            Read = fun r -> r.String
        }
        
    let add (chart: Chart) : string =
        ADD.Execute chart new_db |> expect |> Array.exactlyOne
        
    let private GET_BY_ID: Query<string, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title, Ranked, DifficultyName, Length, ImageLink FROM charts
                WHERE Id = @ChartId;
                """
            Parameters = [
                "@ChartId", SqliteType.Text, -1
            ]
            FillParameters = fun p str -> p.String str
            Read =
                (fun r ->
                {
                    ChartId = r.String
                    DownloadLink = r.String
                    Source = r.String
                    Keymode = r.Int32
                    Difficulty = r.Float32
                    Title = r.String
                    Ranked = r.Int32
                    DifficultyName = r.String
                    Length = r.String
                    ImageLink = r.String
                }
            )
        }
        
    let get_chart_by_id (id: string) : Chart option =
        GET_BY_ID.Execute id new_db |> expect |> Array.tryExactlyOne
        
    let private GET_SOME: Query<int64, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title, Ranked, DifficultyName, Length, ImageLink FROM charts
                LIMIT @limit;
                """
            Parameters = [
                "@limit", SqliteType.Integer, 8
            ]
            FillParameters = fun p int -> p.Int64 int
            Read =
                (fun r ->
                {
                    ChartId = r.String
                    DownloadLink = r.String
                    Source = r.String
                    Keymode = r.Int32
                    Difficulty = r.Float32
                    Title = r.String
                    Ranked = r.Int32
                    DifficultyName = r.String
                    Length = r.String
                    ImageLink = r.String
                }
            )
        }
        
    let get_all =
        GET_SOME.Execute 50000000 new_db |> expect
        
    let get_some(limit: int64) =
        GET_SOME.Execute limit new_db |> expect
        
    let private GET_SOME_RANKED: Query<int64, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title, Ranked, DifficultyName, Length, ImageLink FROM charts
                WHERE Ranked = 1
                LIMIT @limit;
                """
            Parameters = [
                "@limit", SqliteType.Integer, 8
            ]
            FillParameters = fun p int -> p.Int64 int
            Read =
                (fun r ->
                {
                    ChartId = r.String
                    DownloadLink = r.String
                    Source = r.String
                    Keymode = r.Int32
                    Difficulty = r.Float32
                    Title = r.String
                    Ranked = r.Int32
                    DifficultyName = r.String
                    Length = r.String
                    ImageLink = r.String
                }
            )
        }
        
    let get_all_ranked =
        GET_SOME_RANKED.Execute 50000000 new_db |> expect
        
    let get_some_ranked(limit: int64) =
        GET_SOME_RANKED.Execute limit new_db |> expect
        
    let private GET_BY_SOURCE: Query<string, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title, Ranked, DifficultyName, Length, ImageLink FROM charts
                WHERE Source = @Source;
                """
            Parameters = [
                "@Source", SqliteType.Text, -1
            ]
            FillParameters = fun p string -> p.String string
            Read =
                (fun r ->
                {
                    ChartId = r.String
                    DownloadLink = r.String
                    Source = r.String
                    Keymode = r.Int32
                    Difficulty = r.Float32
                    Title = r.String
                    Ranked = r.Int32
                    DifficultyName = r.String
                    Length = r.String
                    ImageLink = r.String
                }
            )
        }
        
    let rec get_by_source(source: string) =
        GET_BY_SOURCE.Execute source new_db |> expect
        
    let private UPDATE: NonQuery<string * Chart> =
        {
            SQL =
                """
                UPDATE charts
                SET
                    DownloadLink = @DownloadLink,
                    Source = @Source,
                    Keymode = @Keymode,
                    Difficulty = @Difficulty,
                    Title = @Title,
                    Ranked = @Ranked,
                    DifficultyName = @DifficultyName,
                    Length = @Length,
                    ImageLink  = @ImageLink
                WHERE Id = @chartId;
            """
            Parameters =
                [
                    "@chartId", SqliteType.Text, -1
                    "@DownloadLink", SqliteType.Text, -1
                    "Source", SqliteType.Text, -1
                    "@Keymode", SqliteType.Integer, 8
                    "@Difficulty", SqliteType.Real, -1
                    "@Title", SqliteType.Text, -1
                    "@Ranked", SqliteType.Integer, 8
                    "@DifficultyName", SqliteType.Text, -1
                    "@Length", SqliteType.Text, -1
                    "@ImageLink", SqliteType.Text, -1
                ]
            FillParameters =
                (fun p (chart_id, chart) ->
                    p.String chart_id
                    p.String chart.DownloadLink
                    p.String chart.Source
                    p.Int64 chart.Keymode
                    p.Float32 chart.Difficulty
                    p.String chart.Title
                    p.Int32 chart.Ranked
                    p.String chart.DifficultyName
                    p.String chart.Length
                    p.String chart.ImageLink
                )
        }
        
    let update (chart_id: string) (chart: Chart): bool =
        UPDATE.Execute (chart_id, chart) new_db |> expect = 1