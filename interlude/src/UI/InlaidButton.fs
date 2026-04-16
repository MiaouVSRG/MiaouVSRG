namespace Interlude.UI

open System.Drawing
open System.Linq
open System.Runtime.CompilerServices
open Interlude.Content
open Interlude.UI
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Prelude.Skins.Themes.Theme

type ButtonType =
    /// Button with rounded corners only at the bottom
    | BottomRounded
    /// Button with rounded corners
    | Default
    /// Button with a custom sprite
    | CustomSprite of string

type InlaidButton(label_func: unit -> string, on_click: unit -> unit, button_type: ButtonType, ?custom_text_shrink: float32 * float32, ?color_func: unit -> Color) =
    inherit
        Container(
            NodeType.Button(fun () ->
                Style.click.Play()
                on_click ()
            )
        )

    static member HEIGHT = 55.0f

    new (label: string, on_click: unit -> unit, button_type: ButtonType) = InlaidButton(K label, on_click, button_type)
    new (label: string, on_click: unit -> unit, button_type: ButtonType, custom_text_shrink: float32 * float32) = InlaidButton(K label, on_click, button_type, custom_text_shrink)
    new (label: string, on_click: unit -> unit, button_type: ButtonType, custom_text_shrink: float32 * float32, color_func: unit -> Color) = InlaidButton(K label, on_click, button_type, custom_text_shrink, color_func)

    member val Hotkey : Hotkey = "none" with get, set
    member val Icon : string = "" with get, set
    member val HoverText : string = label_func() with get, set
    member val HoverIcon : string = "" with get, set
    member val TextColor = Colors.text_witheout with get, set
    
    member val NoHover : bool = false with get, set

    override this.Init(parent) =
        this
            .Add(
                MouseListener().Button(this),
                HotkeyListener(this.Hotkey, fun () ->
                    Style.click.Play()
                    on_click ()
                )
            )

        base.Init parent

    override this.OnFocus(by_mouse: bool) =
        base.OnFocus by_mouse
        Style.hover.Play()

    override this.Draw() =
        let button_texture_name =
            match button_type with
            | Default -> "default-button"
            | BottomRounded -> "default-button-bottomrounded"
            | CustomSprite tex_name -> tex_name

        let button_texture =
            if this.Focused && TEXTURES.Contains(button_texture_name + "-hover") then
                Content.Texture (button_texture_name + "-hover")
            else
                Content.Texture button_texture_name
        
        let q = this.Bounds |> _.AsQuad
        
        if color_func.IsSome then
            let q = this.Bounds.SliceL(254.0f).SliceT(InlaidButton.HEIGHT + 15.0f) |> _.AsQuad
            Render.quad q (color_func.Value())
        
        Render.tex_quad q Colors.white.AsQuad (Sprite.pick_texture (0,0) button_texture)

        let text =
            if this.Focused && not this.NoHover then
                if this.HoverIcon = "" then this.HoverText
                else sprintf "%s %s" this.HoverIcon this.HoverText
            elif this.Icon = "" then label_func()
            else sprintf "%s %s" this.Icon (label_func())

        Text.fill_b (
            Style.font,
            text,
            (match custom_text_shrink with
                | Some shrink -> this.Bounds.Shrink(fst shrink, snd shrink).TranslateY(5.0f)
                | None -> this.Bounds.Shrink(10.0f, 5.0f)
             ),
            (if this.Focused then
                 Colors.text_yellow_2
             else
                 this.TextColor),
            Alignment.CENTER
        )

        base.Draw()

[<Extension>]
type InlaidButtonExtensions =

    [<Extension>]
    static member Hotkey(button: InlaidButton, hotkey: Hotkey) : InlaidButton =
        button.Hotkey <- hotkey
        button

    [<Extension>]
    static member Icon(button: InlaidButton, icon: string) : InlaidButton =
        button.Icon <- icon
        button.HoverIcon <- icon
        button

    [<Extension>]
    static member HoverIcon(button: InlaidButton, icon: string) : InlaidButton =
        button.HoverIcon <- icon
        button

    [<Extension>]
    static member HoverText(button: InlaidButton, text: string) : InlaidButton =
        button.HoverText <- text
        button

    [<Extension>]
    static member TextColor(button: InlaidButton, color: Color) : InlaidButton =
        button.TextColor <- (color, Colors.shadow_2)
        button

    [<Extension>]
    static member TextColor(button: InlaidButton, color: Color * Color) : InlaidButton =
        button.TextColor <- color
        button