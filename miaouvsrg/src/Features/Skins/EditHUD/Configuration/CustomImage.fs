namespace MiaouVSRG.Features.Skins.EditHUD

open Percyqaz.Common
open Percyqaz.Flux.UI
open Catnip
open MiaouVSRG.Content
open MiaouVSRG.UI

type CustomImagePage() =
    inherit Page()

    let config = Content.HUD
    let frame_time =
        config.CustomImageFrameTime
        |> Setting.bounded (10.0f<ms / rate>, 2000.0f<ms / rate>)

    member this.SaveChanges() =
        Skins.save_hud_config
            { Content.HUD with
                CustomImageFrameTime = frame_time.Value
            }

    override this.Content() =
        page_container()
            .With(
                PageSetting(%"hud.custom_image.frame_time", Slider(frame_time |> Setting.uom, Step = 1f))
                    .Pos(0)
            )

    override this.Title = %"hud.custom_image"