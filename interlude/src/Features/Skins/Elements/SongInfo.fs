namespace Interlude.Features.Play.HUD

open Interlude.Content
open Interlude.Features.Gameplay
open Interlude.Features.Play
open Interlude.Features.Play.HUD.ProgressMeter
open Interlude.UI
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Prelude
open Prelude.Skins.HudLayouts

type SongInfo(config: HudConfig, state: PlayState) =
    inherit Container(NodeType.None)
    
    let duration =
        let chart = state.WithColors
        chart.LastNote - chart.FirstNote

    override this.Init(parent) =
        if config.SongInfoShowChartTitle || config.SongInfoShowChartRating then
            this
            |* Text(fun () ->
                if config.SongInfoShowChartTitle && config.SongInfoShowChartRating then
                    $"{state.ChartMeta.Title} (%.2f{state.ChartMeta.Rating})"
                elif config.SongInfoShowChartTitle then
                    state.ChartMeta.Title
                else
                    $"%.2f{state.ChartMeta.Rating}"
            )
                .Color(Colors.text)
                .Align(Alignment.LEFT)
                .Position(Position.SlicePercentT(0.5f).TranslateX(5.0f))

        if config.SongInfoShowChartDifficulty then
            this
            |* Text(state.ChartMeta.DifficultyName)
                .Color(Colors.text_subheading)
                .Align(Alignment.LEFT)
                .Position(Position.ShrinkPercentT(0.4f).SlicePercentT(0.3f).TranslateX(5.0f))
            
        base.Init parent

    override this.Draw() =
        if config.SongInfoShowChartBackground then
            Background.draw_rect (this.Bounds, Color.Transparent)
        
        if config.SongInfoShowCustomBackground then
            let texture = Content.Texture "hud-song-info-background"
            Render.tex_quad 
                (Sprite.fill this.Bounds texture).AsQuad
                Color.White.AsQuad 
                (Sprite.pick_texture (0, 0) texture)
        
        base.Draw()
        
        let now = state.CurrentChartTime()
        
        if config.SongInfoShowChartRemainingTime then
            let time_left = (duration - now) / SelectedChart.rate.Value |> max 0.0f<ms / rate>
            let text = fmt_time_left time_left
            
            Text.fill_b (
                Style.font,
                text,
                this.Bounds.SlicePercentT(0.2f).TranslateY(this.Bounds.Height * 0.6f).TranslateX(5.0f),
                Colors.text_subheading,
                Alignment.LEFT
            )
            
        

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)