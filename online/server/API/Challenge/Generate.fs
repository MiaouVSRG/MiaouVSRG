namespace Interlude.Web.Server.API.Challenge

open System
open System.Linq
open System.Text
open Interlude.Web.Server
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Domain.New
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Challenge.Generate
open NetCoreServer
open Percyqaz.Common

module Generate =
    
    let BEGINNER_COIN_RANGE = (10,50)
    let EASY_COIN_RANGE = (60,100)
    let NORMAL_COIN_RANGE = (110,150)
    let HARD_COIN_RANGE = (160,200)
    let EXPERT_COIN_RANGE = (210,250)
    let MASTER_COIN_RANGE = (260,310)
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "pass"
            require_query_parameter query_params "type"
            let pass = query_params["pass"][0]
            let chall_type = query_params["type"][0]
            if pass <> SECRETS.AutomatedTasksPassword then
                response.ReplyError(401, "nope")
            else
                Logging.Info $"Generating {chall_type} challenges..."
                
                let timestamp =
                    match chall_type with
                    | "hourly" -> DateTime.Now.AddHours(1)
                    | "daily" -> DateTime.Now.AddDays(1)
                    | "weekly" -> DateTime.Now.AddDays(7)
                    | "monthly" -> DateTime.Now.AddMonths(1)
                    | _ -> DateTime.Now
                let end_date = Timestamp.from_datetime timestamp
                
                let charts = Charts.get_all
                let charts_6k = charts |> Array.filter(fun c -> c.Keymode = 6)
                let random = Random()
                
                // Beginner challenge
                let number_of_maps = random.Next(1,3)
                let selected_charts = random.GetItems(charts_6k, 1) |> Array.map(_.ChartId) |> Set.ofArray
                let goal_accuracy =
                    if random.Next(2) = 1 then
                        Some (float (random.NextSingle()))
                    else
                        None
                let beg_chall_type: ChallengeChart =
                    {
                        Charts = selected_charts
                        GoalAccuracy = goal_accuracy
                    }
                    
                let id = ChallengeChart.save_new beg_chall_type
                let chall_name =
                    let mutable sb = StringBuilder()
                    for chartid in selected_charts do
                        let chart = Charts.get_chart_by_id chartid
                        sb <- sb.Append(chart.Value.Title)
                        if selected_charts.Last() <> chartid then
                            sb <- sb.Append(" - ")
                    
                    sb.ToString()
                        
                let beg_chall: Challenge =
                    {
                        Name = chall_name
                        Type = chall_type
                        EndDate = end_date
                        Difficulty = "Beginner"
                        Coins = Some (random.Next(fst BEGINNER_COIN_RANGE, snd BEGINNER_COIN_RANGE))
                        Keymode = 6 // Only 6K for now
                        ChallengeChartId = Some id
                        ChallengeAccuracyId = None
                        ChallengeRatingId = None
                    }
                    
                let id = Challenge.save_new beg_chall
                Logging.Debug $"created challenge with id {id}"
                response.ReplyJson({Success = true}: Response)
        }