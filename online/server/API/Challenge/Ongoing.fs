namespace MiaouVSRG.Web.Server.API.Challenge

open System.Linq
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Challenge.Ongoing
open NetCoreServer

module Ongoing =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let ongoing = Challenge.get_ongoing()
            let mutable challenges: Challenge array = Array.empty
            for challenge in ongoing do
                if challenge.ChallengeChartId.IsSome then
                    let chall_type = "challenge_chart"
                    let challenge_details = ChallengeChart.get_by_id challenge.ChallengeChartId.Value
                    let challenge_chart: ChallengeChartResponse =
                        {
                            Charts = challenge_details.Value.Charts |> Array.ofSeq
                            GoalAccuracy = challenge_details.Value.GoalAccuracy
                        }
                    let add_chall: Challenge =
                        {
                            Name = challenge.Name
                            Type = challenge.Type
                            EndDate = challenge.EndDate
                            Difficulty = challenge.Difficulty
                            Coins = challenge.Coins
                            Keymode = challenge.Keymode
                            ChallengeType = chall_type
                            ChallengeChart = Some challenge_chart
                        }
                    challenges <- challenges.Append(add_chall) |> Array.ofSeq
                    
            response.ReplyJson(challenges)
        }