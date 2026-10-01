namespace MiaouVSRG.Features.Play.HUD

open Catnip
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Catnip.Skins.HudLayouts
open MiaouVSRG.Content
open MiaouVSRG.Features.Play

type CustomImage(config: HudConfig, state: PlayState) =
    inherit Container(NodeType.None)

    let animation = Animation.Counter(float config.CustomImageFrameTime)

    let texture = Content.Texture "custom-image"

    override this.Draw() =
        Render.tex_quad 
            ((Sprite.fill this.Bounds texture).AsQuad) 
            Color.White.AsQuad 
            (Sprite.pick_texture (animation.Loops % texture.Columns, 0) texture)

    override this.Update(elapsed_ms, moved) =
        animation.Update(elapsed_ms)
        base.Update(elapsed_ms, moved)