namespace Interlude.Features.OptionsScreen

open System.Drawing
open Interlude.Content
open Interlude.UI
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI

type NewOptionsScreen() =
    inherit Screen()
    
    let keys = FlowContainer.Vertical<Text>(10.0f, Spacing = 2.0f)
    let scroll_container = ScrollContainer(keys)
    
    let fade = Animation.Fade 0.0f

    override this.OnBack() =
        (Screen.change ScreenType.MainMenu Transitions.Raw) |> ignore
        None
        
    override this.OnEnter(var0) = fade.Target <- 1.0f
        
    override this.OnExit(var0) = ()
    
    override this.Init(parent: Widget) =
        while keys.Count < 1000 do
            keys.Add(Text("horus anus chibrus crevette"))
            
        this
            .Add(
                scroll_container
            )
        base.Init(parent)
    
    override this.Update(elapsed_ms, moved) =
        fade.Update elapsed_ms
        base.Update(elapsed_ms, moved)
    
    override this.Draw() =
        if fade.Value > 0.0f then
            let alpha = 200.0f * fade.Value |> int
            Render.rect this.Bounds (Color.Black.O4a(alpha))
        
            let frame = Content.Texture "settings-frame"
            
            let frame_bounds = Rect.FromSize(
                // Center the frame horizontally
                (Render.width() - float32 frame.Width) / 2.0f,
                // Center the frame vertically, + cool slide-in animation from the bottom
                Render.height() - fade.Value * (Render.height() - ((Render.height() - float32 frame.Height) / 2.0f)),
                float32 frame.Width,
                float32 frame.Height
            )
            
            Render.sprite frame_bounds Color.White frame
            
        base.Draw()

