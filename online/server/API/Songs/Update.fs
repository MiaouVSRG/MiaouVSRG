namespace MiaouVSRG.Web.Server.API.Songs

open NetCoreServer
open Percyqaz.Common
open Catnip
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API
open MiaouVSRG.Web.Server.Domain.Core
open MiaouVSRG.Web.Server.Domain.Backbeat
open MiaouVSRG.Web.Server.Bot

module Update =

    open Songs.Update

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let user_id, user = authorize headers

            if not (user.Badges.Contains Badge.DEVELOPER) then
                raise PermissionDeniedException

            match JSON.FromString body with
            | Error e -> raise (BadRequestException None)
            | Ok(request: Request) ->

            match Songs.song_by_id request.SongId with
            | Some (existing_song) ->
                Songs.update_song request.SongId (existing_song.MergeWithIncoming request.Song) |> ignore
                Logging.Info "Accepted changes to existing song '%s'" existing_song.Title
                response.ReplyJson(true)
            | None ->
                response.ReplyJson(false)
        }