namespace MiaouVSRG.Features.LevelSelect

open MiaouVSRG.Content
open MiaouVSRG.Features.Play
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Catnip
open MiaouVSRG.UI
open MiaouVSRG.Features.Gameplay

module MiniChartInfo =
    let SHOW_POS = Position.ShrinkT(10.0f).SliceT(130.0f).ShrinkPercentL(INFO_SCREEN_SPLIT * 0.8f).SliceL(150.0f)
    let HIDDEN_POS = Position.ShrinkT(10.0f).SliceT(130.0f).ShrinkPercentL(INFO_SCREEN_SPLIT * 0.8f).SliceL(150.0f).TranslateX(-SCREEN_OFFSET + 2000.0f)

type MiniChartInfo() =
    inherit SlideContainer(NodeType.None)
    
    let mutable old_rating = 0.0f

    override this.Draw() =
        let chart_namebox_texture = Content.Texture "minichartinfo"
        Render.sprite
            this.Bounds
            Color.White
            chart_namebox_texture
            
        let rate_text = sprintf "x%.2f" SelectedChart.rate.Value
        Text.fill_b (Style.font, rate_text, this.Bounds.Shrink(10.0f, 10.0f).SliceT(30.0f), Colors.text_no_shadow, Alignment.CENTER)
        
        let rating =
            match SelectedChart.DIFFICULTY with
            | None -> old_rating
            | Some d ->
                old_rating <- d.Overall
                d.Overall
            
        Text.fill_b (Style.font, sprintf "%.2f" rating, this.Bounds.Shrink(20.0f, 15.0f).SliceY(60.0f).TranslateY(5.0f), Colors.text_no_shadow, Alignment.LEFT)
        
        let star = Content.Texture "star"
        Render.sprite (this.Bounds.Shrink(20.0f, 15.0f).SliceY(32.0f).SliceR(32.0f)) Color.LightGoldenrodYellow star
        
        Text.fill_b (Style.font, "RANKED", this.Bounds.ShrinkB(10.0f).SliceB(30.0f), Colors.text_no_shadow, Alignment.CENTER)
        
        base.Draw()
        
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        this.Position  <- MiniChartInfo.HIDDEN_POS
        
    member this.Show() : unit =
        this.Position <- MiniChartInfo.SHOW_POS