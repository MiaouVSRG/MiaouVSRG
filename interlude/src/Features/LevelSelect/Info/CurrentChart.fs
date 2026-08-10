namespace Interlude.Features.LevelSelect

open Interlude.Content
open Interlude.Features.Play
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Prelude
open Interlude.UI
open Interlude.Features.Gameplay

type CurrentChart() =
    inherit Container(NodeType.None)
    
    override this.Init(parent: Widget) =
        this
            .Add(
                InlaidButton(
                    Icons.TARGET,
                    (fun () ->
                        SelectedChart.when_loaded true
                        <| fun info ->
                            Screen.change_new
                                (fun () -> PracticeScreen.Create(info, 0.0f<ms>))
                                ScreenType.Practice
                                Transitions.Default
                            |> ignore
                    ),
                    ButtonType.Transparent,
                    (0.0f, 0.0f) // No shrink so the Icon can take all the space
                )
                    .Hotkey("practice_mode")
                    .Position(
                        Position.SliceR(60.0f).SliceB(60.0f).TranslateX(-20.0f).TranslateY(-5.0f)
                    )
                    .Help(Help.Info("levelselect.practice_mode", "practice_mode"))
                )
        
        base.Init(parent)

    override this.Draw() =

        let q = this.Bounds.ShrinkX(10.0f).ShrinkT(10.0f) |> _.AsQuad
        let chart_namebox_texture = Content.Texture "chart-namebox"
        Render.tex_quad
            q
            Color.White.AsQuad
            (Sprite.pick_texture (0,0) chart_namebox_texture)

        let title_text =
            match SelectedChart.CACHE_DATA with
            | None -> %"jukebox.no_chart_selected"
            | Some c -> c.Title
        Text.fill_b (Style.font, title_text, this.Bounds.Shrink(20.0f, 10.0f).SliceT(80.0f), Colors.text, Alignment.CENTER)

        let diff_text =
            match SelectedChart.CACHE_DATA with
            | None -> "--"
            | Some c ->
                if c.Audio.IsAbsolute then Icons.LINK + " " + c.OriginString
                else c.OriginString
        Text.fill_b (Style.font, diff_text, this.Bounds.Shrink(20.0f, 10.0f).SliceB(50.0f), Colors.text, Alignment.CENTER)
        
        base.Draw()