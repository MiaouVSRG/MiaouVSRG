namespace Interlude.Web.Server.API

open System.Collections.Generic
open NetCoreServer
open Percyqaz.Common
open Interlude.Web.Shared
open Interlude.Web.Shared.API
open Interlude.Web.Shared.Requests
open Interlude.Web.Server
open Interlude.Web.Server.API

module API =

    type HandlerBodyString = string * Map<string, string array> * Map<string, string> * HttpResponse -> Async<unit>
    type HandlerBodyBytes = byte array * Map<string, string array> * Map<string, string> * HttpResponse -> Async<unit>
    
    type BodyType =
        | Bytes of HandlerBodyBytes
        | String of HandlerBodyString
        
    let handlers = Dictionary<(HttpMethod * string), BodyType>()

    let inline add_endpoint route handle = handlers.Add(route, handle)

    do
        add_endpoint Health.Status.ROUTE (BodyType.String Health.Status.handle)

        if not SECRETS.IsProduction then
            add_endpoint (GET, "/auth/dummy") (BodyType.String Auth.Dummy.handle)
            add_endpoint New.Charts.Migrate.ROUTE (BodyType.String New.Charts.Migrate.handle)

        add_endpoint Auth.Discord.ROUTE (BodyType.String Auth.Discord.handle)

        add_endpoint Charts.Identify.ROUTE (BodyType.String Charts.Identify.handle)
        add_endpoint Charts.Add.ROUTE (BodyType.String Charts.Add.handle)
        add_endpoint Charts.Scores.Save.ROUTE (BodyType.String Charts.Scores.Save.handle)
        add_endpoint Charts.Scores.Leaderboard.ROUTE (BodyType.String Charts.Scores.Leaderboard.handle)

        add_endpoint Songs.Search.ROUTE (BodyType.String Songs.Search.handle)
        add_endpoint Songs.Scan.ROUTE (BodyType.String Songs.Scan.handle)
        add_endpoint Songs.Update.ROUTE (BodyType.String Songs.Update.handle)

        add_endpoint Tables.Records.ROUTE (BodyType.String Tables.Records.handle)
        add_endpoint Tables.Leaderboard.ROUTE (BodyType.String Tables.Leaderboard.handle)
        add_endpoint Tables.List.ROUTE (BodyType.String Tables.List.handle)
        add_endpoint Tables.Charts.ROUTE (BodyType.String Tables.Charts.handle)

        add_endpoint Tables.Suggestions.Vote.ROUTE (BodyType.String Tables.Suggestions.Vote.handle)
        add_endpoint Tables.Suggestions.List.ROUTE (BodyType.String Tables.Suggestions.List.handle)
        add_endpoint Tables.Suggestions.Missing.ROUTE (BodyType.String Tables.Suggestions.Missing.handle)
        add_endpoint Tables.Suggestions.Accept.ROUTE (BodyType.String Tables.Suggestions.Accept.handle)
        add_endpoint Tables.Suggestions.Reject.ROUTE (BodyType.String Tables.Suggestions.Reject.handle)

        add_endpoint Players.Online.ROUTE (BodyType.String Players.Online.handle)
        add_endpoint Players.Search.ROUTE (BodyType.String Players.Search.handle)

        add_endpoint Players.Profile.View.ROUTE (BodyType.String Players.Profile.View.handle)
        add_endpoint Players.Profile.Options.ROUTE (BodyType.String Players.Profile.Options.handle)

        add_endpoint Friends.List.ROUTE (BodyType.String Friends.List.handle)
        add_endpoint Friends.Add.ROUTE (BodyType.String Friends.Add.handle)
        add_endpoint Friends.Remove.ROUTE (BodyType.String Friends.Remove.handle)

        add_endpoint Stats.Sync.ROUTE (BodyType.String Stats.Sync.handle)
        add_endpoint Stats.Fetch.ROUTE (BodyType.String Stats.Fetch.handle)
        add_endpoint Stats.Leaderboard.XP.ROUTE (BodyType.String Stats.Leaderboard.XP.handle)
        add_endpoint Stats.Leaderboard.MonthlyXP.ROUTE (BodyType.String Stats.Leaderboard.MonthlyXP.handle)
        add_endpoint Stats.Leaderboard.Keymode.ROUTE (BodyType.String Stats.Leaderboard.Keymode.handle)
        add_endpoint Stats.Leaderboard.MonthlyKeymode.ROUTE (BodyType.String Stats.Leaderboard.MonthlyKeymode.handle)
        
        add_endpoint New.Charts.Add.ROUTE (BodyType.String New.Charts.Add.handle)
        add_endpoint New.Charts.Download.ROUTE (BodyType.String New.Charts.Download.handle)
        
        // WEBSITE REQUESTS
        add_endpoint Web.Auth.Discord.ROUTE (BodyType.String Web.Auth.Discord.handle)
        add_endpoint Web.Auth.Discord.Finish.ROUTE (BodyType.String Web.Auth.Finish.handle)
        add_endpoint Web.Auth.Verify.ROUTE (BodyType.String Web.Auth.Verify.handle)
        add_endpoint Web.Auth.Validate.ROUTE (BodyType.String Web.Auth.Validate.handle)
        
        add_endpoint Web.User.Search.ROUTE (BodyType.String Web.Users.Search.handle)
        add_endpoint Web.User.Login.ROUTE (BodyType.String Web.Users.Login.handle)
        add_endpoint Web.User.Register.ROUTE (BodyType.String Web.Users.Register.handle)
        add_endpoint Web.User.Completion.ROUTE (BodyType.String Web.Users.Completion.handle)
        add_endpoint Web.User.Upload.ROUTE (BodyType.Bytes Web.Users.Upload.handle)
        add_endpoint Web.Leaderboard.ROUTE (BodyType.String Web.Leaderboard.handle)
        add_endpoint Web.Map.Info.ROUTE (BodyType.String Web.Maps.Info.handle)
        add_endpoint Web.Map.Leaderboard.ROUTE (BodyType.String Web.Maps.Leaderboard.handle)

    let handle_request
        (
            method: HttpMethod,
            route: string,
            body: string,
            body_bytes: byte array,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            if handlers.ContainsKey((method, route)) then
                try
                    let handler = handlers.[(method, route)]
                    let callback =
                        match handler with
                        | BodyType.Bytes handler_bytes -> handler_bytes (body_bytes, query_params, headers, response)
                        | BodyType.String handler_string -> handler_string (body, query_params, headers, response)
                    do! callback
                with
                | :? NotAuthorizedException ->
                    response.ReplyError(401, "Missing authorization token") |> ignore
                | :? NotFoundException ->
                    response.ReplyError(404, "Not found") |> ignore
                | :? AuthorizeFailedException ->
                    response.ReplyError(403, "Bad authorization token") |> ignore
                | :? PermissionDeniedException ->
                    response.ReplyError(403, "Permission denied") |> ignore
                | :? BadRequestException as err ->
                    response.ReplyError(400, Option.defaultValue "Bad request" err.Message)
                    |> ignore
                | err ->
                    Logging.Error "Unhandled exception in %O %s: %O" method route err
                    Discord.debug_log (sprintf "Unhandled exception in %O %s\n%s" method route (err.ToString()))
                    response.ReplyError(500, "Internal error") |> ignore
            else
                response.ReplyError(404, "Route not found") |> ignore
        }