namespace Interlude.Web.Server.Domain.Core

open Interlude.Web.Server
open Percyqaz.Common
open Percyqaz.Data.Sqlite
open Prelude

type Challenge =
    {
        Name: string
        Type: string
        EndDate: int64
        Difficulty: string
        Coins: int64 option
        Keymode: int
        ChallengeChartId: int64 option
        ChallengeAccuracyId: int64 option
        ChallengeRatingId: int64 option
    }
    
    member this.WithChallangeChartId(challenge_chart_id: int64) =
        {this with ChallengeChartId = Some challenge_chart_id}
        
    member this.WithChallangeAccuracyId(challenge_accuracy_id: int64) =
        {this with ChallengeAccuracyId = Some challenge_accuracy_id}
        
    member this.WithChallangeRatingId(challenge_rating_id: int64) =
        {this with ChallengeRatingId = Some challenge_rating_id}
        
    member this.IsFinished() = this.EndDate < Timestamp.now()
    
module Challenge =
    let internal TABLE: TableCommandHelper =
        {
            Name = "challenges"
            PrimaryKey = Column.Integer("ChallengeId").Unique
            Columns =
                [
                    Column.Text("Name")
                    Column.Text("Type")
                    Column.Integer("EndDate")
                    Column.Text("Difficulty")
                    Column.Integer("Coins").Nullable
                    Column.Integer("Keymode")
                    Column.Integer("ChallengeChartId").Nullable
                    Column.Integer("ChallengeAccuracyId").Nullable
                    Column.Integer("ChallengeRatingId").Nullable
                ]
        }
        
    let private SAVE_NEW: NonQuery<Challenge> =
        {
            SQL = TABLE.INSERT
            Parameters =
                [
                    "@Name", SqliteType.Text, -1
                    "@Type", SqliteType.Text, -1
                    "@EndDate", SqliteType.Integer, 8
                    "@Difficulty", SqliteType.Text, -1
                    "@Coins", SqliteType.Integer, 8
                    "@Keymode", SqliteType.Integer, 8
                    "@ChallengeChartId", SqliteType.Integer, 8
                    "@ChallengeAccuracyId", SqliteType.Integer, 8
                    "@ChallengeRatingId", SqliteType.Integer, 8
                ]
            FillParameters =
                (fun p challenge ->
                    p.String challenge.Name
                    p.String challenge.Type
                    p.Int64 challenge.EndDate
                    p.String challenge.Difficulty
                    p.Int64Option challenge.Coins
                    p.Int32 challenge.Keymode
                    p.Int64Option challenge.ChallengeChartId
                    p.Int64Option challenge.ChallengeAccuracyId
                    p.Int64Option challenge.ChallengeRatingId
                )
        }

    let save_new (challenge: Challenge) : int64 =
        SAVE_NEW.ExecuteGetId challenge core_db |> expect
        
    let private BY_ID: Query<int64, Challenge> =
        {
            SQL = """SELECT * FROM challenges WHERE ChallengeId = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8 ]
            FillParameters = (fun p id -> p.Int64 id)
            Read =
                (fun r ->
                    r.Int64 |> ignore
                    {
                        Name = r.String
                        Type = r.String
                        EndDate = r.Int64
                        Difficulty = r.String
                        Coins = r.Int64Option
                        Keymode = r.Int32
                        ChallengeChartId = r.Int64Option
                        ChallengeAccuracyId = r.Int64Option
                        ChallengeRatingId = r.Int64Option
                    }
                )
        }

    let by_id (id: int64) =
        BY_ID.Execute id core_db |> expect |> Array.tryExactlyOne
        
    let private GET_SOME: Query<int64, Challenge> =
        {
            SQL = """SELECT Name, Type, EndDate, Difficulty, Coins, Keymode, ChallengeChartId, ChallengeAccuracyId, ChallengeRatingId 
                        FROM challenges 
                        LIMIT @Limit;
                """
            Parameters = ["@Limit", SqliteType.Integer, 8]
            FillParameters = (fun p limit -> p.Int64 limit)
            Read =
                (fun r ->
                    {
                        Name = r.String
                        Type = r.String
                        EndDate = r.Int64
                        Difficulty = r.String
                        Coins = r.Int64Option
                        Keymode = r.Int32
                        ChallengeChartId = r.Int64Option
                        ChallengeAccuracyId = r.Int64Option
                        ChallengeRatingId = r.Int64Option
                    }
                )
        }
        
    let get_all () =
        GET_SOME.Execute 50000000 core_db |> expect
    
    let private GET_ONGOING: Query<int64, Challenge> =
        {
            SQL = """SELECT Name, Type, EndDate, Difficulty, Coins, Keymode, ChallengeChartId, ChallengeAccuracyId, ChallengeRatingId 
                        FROM challenges 
                        WHERE EndDate >= @now;
                """
            Parameters = ["@now", SqliteType.Integer, 8]
            FillParameters = (fun p now -> p.Int64 now)
            Read =
                (fun r ->
                    {
                        Name = r.String
                        Type = r.String
                        EndDate = r.Int64
                        Difficulty = r.String
                        Coins = r.Int64Option
                        Keymode = r.Int32
                        ChallengeChartId = r.Int64Option
                        ChallengeAccuracyId = r.Int64Option
                        ChallengeRatingId = r.Int64Option
                    }
                )
        }
        
    let get_ongoing () =
        let now = Timestamp.now()
        GET_ONGOING.Execute now core_db |> expect
        
        
type ChallengeChart =
    {
        Charts: Set<string>
        GoalAccuracy: float option
    }
    
module ChallengeChart =
    let internal TABLE: TableCommandHelper =
        {
            Name = "challenge_chart"
            PrimaryKey = Column.Integer("ChallengeChartId").Unique
            Columns =
                [
                    Column.Text("Charts")
                    Column.Real("GoalAccuracy").Nullable
                ]
        }
        
    let private SAVE_NEW: NonQuery<ChallengeChart> =
        {
            SQL = TABLE.INSERT
            Parameters =
                [
                    "@Charts", SqliteType.Text, -1
                    "@GoalAccuracy", SqliteType.Real, 8
                ]
            FillParameters =
                (fun p challenge ->
                    p.Json JSON challenge.Charts
                    p.Float64Option challenge.GoalAccuracy
                )
        }

    let save_new (challenge: ChallengeChart) : int64 =
        SAVE_NEW.ExecuteGetId challenge core_db |> expect
        
    let private GET_BY_ID: Query<int64, ChallengeChart> =
        {
            SQL = "SELECT [Charts], GoalAccuracy FROM challenge_chart WHERE ChallengeChartId = @ChallengeChartId"
            Parameters = [ "@ChallengeChartId", SqliteType.Integer, 8 ]
            FillParameters = fun p id -> p.Int64 id
            Read = (fun r ->
                    {
                        Charts = r.Json JSON
                        GoalAccuracy = r.Float64Option
                    }
                )
        }
        
    let get_by_id(id: int64) =
        GET_BY_ID.Execute id core_db |> expect |> Array.tryExactlyOne