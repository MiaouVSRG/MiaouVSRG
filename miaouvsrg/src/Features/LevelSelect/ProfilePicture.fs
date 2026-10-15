namespace MiaouVSRG.Features.LevelSelect

open Catnip
open Catnip.Data
open MiaouVSRG.Features.Skins.Browser
open MiaouVSRG.Web.Shared.Requests
open Percyqaz.Common
open Percyqaz.Flux.UI
open Percyqaz.Flux.Windowing

type ProfilePicture(username: string) =
    inherit Container(NodeType.Button(fun () -> open_url($"https://www.beta.miaouvsrg.com/user/profile/{username}")))
    
    let thumbnail =
        { new Thumbnail() with
            override this.Load() =
                Players.ProfilePicture.get (
                     username,
                     fun response ->
                         GameThread.defer
                         <| fun () ->
                             match response with
                             | Some result ->
                                 ImageServices.get_cached_image.Request(
                                    result.ProfilePictureLink,
                                    function
                                    | Some img -> GameThread.defer (fun () -> this.FinishLoading(img, $"AVATAR_PREVIEW_{username.ToUpper()}"))
                                    | None -> Logging.Warn "Failed to load avatar thumbnail from '%s'" result.ProfilePictureLink
                                )
                             | None -> ()
                )
        }
        
    override this.Init(parent: Widget) =
        this.Add(thumbnail)
        base.Init(parent)
