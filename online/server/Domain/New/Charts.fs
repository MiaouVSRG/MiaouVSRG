namespace Interlude.Web.Server.Domain.New

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
        Difficulty: string
    }
    member this.FormatSource() = {
        this with Source = if this.Source.Equals("osu!") || this.Source.Equals("Etterna") || this.Source.Equals("BMS") || this.Source.Equals("o2jam") then this.Source else "none"
    }

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
                Difficulty TEXT NOT NULL,
                Title TEXT NOT NULL
            );
            """
        }
        
    let private ADD: Query<Chart, string> =
        {
            SQL =
                """
            INSERT INTO charts (Id, DownloadLink, Source, Keymode, Difficulty, Title)
            VALUES (@ChartId, @DownloadLink, @Source, @Keymode, @Difficulty, @Title)
            RETURNING Id;
            """
            Parameters =
                [
                    "@ChartId", SqliteType.Text, -1
                    "@DownloadLink", SqliteType.Text, -1
                    "@Source", SqliteType.Text, -1
                    "@Keymode", SqliteType.Integer, 8
                    "@Difficulty", SqliteType.Text, -1
                    "@Title", SqliteType.Text, -1
                ]
            FillParameters =
                (fun p chart ->
                    p.String chart.ChartId
                    p.String chart.DownloadLink
                    p.String chart.Source
                    p.Int64 chart.Keymode
                    p.String chart.Difficulty
                    p.String chart.Title
                )
            Read = fun r -> r.String
        }
        
    let add (chart: Chart) : string =
        ADD.Execute chart new_db |> expect |> Array.exactlyOne
        
    let private GET_BY_ID: Query<string, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title FROM charts
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
                    Difficulty = r.String
                    Title = r.String
                }
            )
        }
        
    let get_chart_by_id (id: string) : Chart option =
        GET_BY_ID.Execute id new_db |> expect |> Array.tryExactlyOne
        
    let private GET_SOME: Query<int64, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title FROM charts
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
                    Difficulty = r.String
                    Title = r.String
                }
            )
        }
        
    let get_all =
        GET_SOME.Execute 50000000 new_db |> expect
        
    let private GET_BY_SOURCE: Query<string, Chart> =
        {
            SQL =
                """
                SELECT Id, DownloadLink, Source, Keymode, Difficulty, Title FROM charts
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
                    Difficulty = r.String
                    Title = r.String
                }
            )
        }
        
    let rec get_by_source(source: string) =
        GET_BY_SOURCE.Execute source new_db |> expect