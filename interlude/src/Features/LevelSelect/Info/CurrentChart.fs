namespace Interlude.Features.LevelSelect

open Interlude.Content
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Prelude
open Interlude.UI
open Interlude.Features.Gameplay

type CurrentChart() =
    inherit StaticWidget(NodeType.None)

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