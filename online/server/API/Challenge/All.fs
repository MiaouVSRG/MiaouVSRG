namespace MiaouVSRG.Web.Server.API.Challenge

open System.Linq
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Challenge.All
open NetCoreServer
open Percyqaz.Common

module All =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let challenges = Challenge.get_all()
            let mutable challenges_res: ChallengeResponse array = Array.empty
            for challenge in challenges do
                let res: ChallengeResponse =
                    {
                        Name = challenge.Name
                        Type = challenge.Type
                        EndDate = challenge.EndDate
                        Difficulty = challenge.Difficulty
                        Coins = challenge.Coins
                        Keymode = challenge.Keymode
                        Ongoing = Timestamp.now() <= challenge.EndDate
                    }
                challenges_res <- challenges_res.Append(res) |> Array.ofSeq
                
            response.ReplyJson(challenges_res)
        }