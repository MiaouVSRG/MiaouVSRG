namespace Interlude.Web.Server.Web.Users

open Interlude.Web.Shared.Requests.Web.User.Friends
open NetCoreServer
open Interlude.Web.Shared
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Server.Online

module Friends =

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            let token = require_cookie headers "token"
            match User.by_auth_token token with
            | Some (id, user) ->

                let friends = Friends.friends_list id

                let online =
                    Session.find_session_ids_by_usernames (friends |> Array.map (fun (id, u) -> u.Username))
                    
                let friends =
                    Array.zip friends online
                    |> Array.map (fun ((friend_id, friend), session) ->
                        let relation = Friends.relation(id, friend_id)
                        {
                            Username = friend.Username
                            IsOnline = session.IsSome
                            Avatar = friend.ProfilePicture
                            Banner = friend.ProfileBanner
                            Country = friend.CountryFlag
                            IsMutual = relation.IsMutualFriend
                        }
                    )

                response.ReplyJson(friends, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
            | None ->
                response.ReplyError(404, "User not found !")
        }