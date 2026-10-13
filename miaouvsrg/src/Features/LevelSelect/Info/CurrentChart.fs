namespace MiaouVSRG.Features.LevelSelect

open MiaouVSRG.Content
open MiaouVSRG.Features.Play
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Catnip
open MiaouVSRG.UI
open MiaouVSRG.Features.Gameplay

module CurrentChart =
    let SHOW_POS = Position.SliceT(TOP_BAR_HEIGHT + 50.0f).SlicePercentL(INFO_SCREEN_SPLIT * 0.8f)
    let HIDDEN_POS = Position.SliceT(TOP_BAR_HEIGHT + 50.0f).SlicePercentL(INFO_SCREEN_SPLIT * 0.8f).TranslateX(-SCREEN_OFFSET + 2000.0f)

type CurrentChart() =
    inherit SlideContainer(NodeType.None)
    
    let mutable rice = ""
    let mutable hold = ""
    
    let refresh(info: LoadedChartInfo) =
        rice <- $"{info.RiceCount}"
        hold <- $"{info.HoldCount}"
    
    override this.Init(parent: Widget) =
        SelectedChart.on_chart_change_finished.Add refresh
        SelectedChart.on_chart_update_finished.Add refresh
        SelectedChart.if_loaded refresh
        // this
            // .With(
            //     InlaidButton(
            //         Icons.GLOBE,
            //         (fun () ->
            //             let link =
            //                 match SelectedChart.CACHE_DATA with
            //                 | None -> "404"
            //                 | Some c ->
            //                     c.Hash
            //             open_url($"https://www.beta.miaouvsrg.com/charts/chartpage/{link}")
            //         ),
            //         ButtonType.Transparent,
            //         (0.0f, 0.0f) // No shrink so the Icon can take all the space
            //     )
            //         .Position(
            //             Position.SliceL(60.0f).SliceB(60.0f).TranslateX(20.0f).TranslateY(-5.0f)
            //         )
            // )
            // .Add(
            //     InlaidButton(
            //         Icons.TARGET,
            //         (fun () ->
            //             SelectedChart.when_loaded true
            //             <| fun info ->
            //                 Screen.change_new
            //                     (fun () -> PracticeScreen.Create(info, 0.0f<ms>))
            //                     ScreenType.Practice
            //                     Transitions.Default
            //                 |> ignore
            //         ),
            //         ButtonType.Transparent,
            //         (0.0f, 0.0f) // No shrink so the Icon can take all the space
            //     )
            //         .Hotkey("practice_mode")
            //         .Position(
            //             Position.SliceR(60.0f).SliceB(60.0f).TranslateX(-20.0f).TranslateY(-5.0f)
            //         )
            //         .Help(Help.Info("levelselect.practice_mode", "practice_mode"))
            //     )
        
        base.Init(parent)

    override this.Draw() =
        let bounds = this.Bounds.ShrinkX(10.0f).ShrinkT(10.0f)
        let chart_namebox_texture = Content.Texture "chart-namebox"
        Render.sprite
            bounds
            Color.White
            chart_namebox_texture

        let title_text =
            match SelectedChart.CACHE_DATA with
            | None -> %"jukebox.no_chart_selected"
            | Some c -> c.Title
        Text.fill_b (Style.font, title_text, this.Bounds.Shrink(35.0f, 10.0f).SliceT(80.0f), Colors.text, Alignment.CENTER)

        let diff_text =
            match SelectedChart.CACHE_DATA with
            | None -> ""
            | Some c ->
                c.DifficultyName
                
        Text.fill_b (Style.font, diff_text, this.Bounds.Shrink(35.0f, 10.0f).ShrinkT(70.0f).SliceT(50.0f), Colors.text, Alignment.CENTER)
        
        // let rate_text = sprintf "x%.2f" SelectedChart.rate.Value
        
        // Text.fill_b (Style.font, rate_text, this.Bounds.Shrink(35.0f, 10.0f).ShrinkT(110.0f).SliceT(30.0f), Colors.text_no_shadow, Alignment.CENTER)
        
        let time_sprite = Content.Texture "time"
        let bpm_sprite = Content.Texture "bpm"
        let rice_sprite = Content.Texture "rice"
        let ln_sprite = Content.Texture "ln"
        
        let base_bounds = this.Bounds.Shrink(100.0f, 10.0f).SliceB(40.0f)
        let grid_x (pos: float32) =
            let cols = 4.0f
            base_bounds.ShrinkPercentL(1.0f / cols * (pos - 1.0f)).SlicePercentL(1.0f / (cols - pos + 1.0f))
            
        Text.fill_b (Style.font, SelectedChart.FMT_DURATION, grid_x(1.0f).SlicePercentL(0.8f), Colors.text_no_shadow, Alignment.RIGHT)
        Render.sprite (grid_x(1.0f).SliceR(35.0f).SliceY(35.0f).TranslateX(20.0f).TranslateY(-3.0f)) Color.White time_sprite
        
        Text.fill_b (Style.font, SelectedChart.FMT_BPM, grid_x(2.0f).SlicePercentL(0.8f).TranslateX(-10.0f), Colors.text_no_shadow, Alignment.RIGHT)
        Render.sprite (grid_x(2.0f).SliceR(35.0f).SliceY(35.0f).TranslateX(10.0f).TranslateY(-3.0f)) Color.White bpm_sprite
        
        Text.fill_b (Style.font, rice, grid_x(3.0f).SlicePercentL(0.8f).TranslateX(5.0f), Colors.text_no_shadow, Alignment.RIGHT)
        Render.sprite (grid_x(3.0f).SliceR(35.0f).SliceY(35.0f).TranslateX(25.0f).TranslateY(-3.0f)) Color.White rice_sprite
        
        Text.fill_b (Style.font, hold, grid_x(4.0f).SlicePercentL(0.8f).TranslateX(5.0f), Colors.text_no_shadow, Alignment.RIGHT)
        Render.sprite (grid_x(4.0f).SliceR(35.0f).SliceY(35.0f).TranslateX(25.0f).TranslateY(-3.0f)) Color.White ln_sprite
        
        base.Draw()
        
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        this.Position  <- CurrentChart.HIDDEN_POS
        
    member this.Show() : unit =
        this.Position <- CurrentChart.SHOW_POS