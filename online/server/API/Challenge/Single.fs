namespace MiaouVSRG.Web.Server.API.Challenge

open System.Linq
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests.Challenge.Single
open NetCoreServer
open Percyqaz.Common

module Single =
    
    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            require_query_parameter query_params "id"
            let id = query_params["id"][0] |> int64
            match Challenge.by_id id with
            | Some challenge ->
                let res: Response =
                    {
                        Name = challenge.Name
                        Type = challenge.Type
                        EndDate = challenge.EndDate
                        Difficulty = challenge.Difficulty
                        Coins = challenge.Coins
                        Keymode = challenge.Keymode
                        Ongoing = Timestamp.now() <= challenge.EndDate
                    }
                response.ReplyJson(res)
            | None ->
                response.ReplyError(404, "Challenge not found")
        }