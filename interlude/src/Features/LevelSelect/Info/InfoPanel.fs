namespace Interlude.Features.LevelSelect

open Interlude.Content
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Prelude
open Interlude.Features.Rulesets
open Interlude.Options
open Interlude.UI
open Interlude.Features.Gameplay

module GameplayUtils =
    let SHOW_POS = Position.SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 20.0f)
    let HIDDEN_POS = Position.SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 20.0f).TranslateX(-SCREEN_OFFSET)

type GameplayUtils(change_rate: Rate -> unit) =
    inherit SlideContainer(NodeType.None)
    
    override this.Init(parent: Widget) =
        this
            .Add(
                InlaidButton(
                    sprintf "%s %s" Icons.EYE %"levelselect.preview", 
                    (fun () -> SelectedChart.when_loaded false <| fun info -> Preview(info, change_rate).Show()),
                    ButtonType.CustomSprite "preview-button",
                    (13.0f, 15.0f)
                )
                    .Hotkey("preview")
                    .Position(
                        Position
                            .SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 20.0f)
                            .GridX(1, 3, 0.0f)
                    )
                    .Help(Help.Info("levelselect.preview", "preview")),

                ModSelect(change_rate)
                    .Position(
                        Position
                            .SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 20.0f)
                            .GridX(2, 3, 0.0f)
                    )
                    .Help(Help.Info("levelselect.mods", "mods")),

                RulesetSwitcher(options.SelectedRuleset)
                    .Position(
                        Position 
                            .SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 20.0f)
                            .GridX(3, 3, 0.0f)
                    )
                    .Help(Help.Info("levelselect.rulesets", "ruleset_switch"))
            )
        base.Init(parent)
        
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        this.Position  <- GameplayUtils.HIDDEN_POS
        
    member this.Show() : unit =
        this.Position <- GameplayUtils.SHOW_POS

module MainDisplay =
    let SHOW_POS = Position.ShrinkB(GameplayInfo.HEIGHT + AngledButton.HEIGHT + Style.PADDING)
    let HIDDEN_POS = Position.ShrinkB(GameplayInfo.HEIGHT + AngledButton.HEIGHT + Style.PADDING).TranslateX(-SCREEN_OFFSET)
    
type MainDisplay(info_panel_mode: Setting<InfoPanelMode,unit>) =
    inherit SlideContainer(NodeType.None)
    
    let scoreboard = Scoreboard(info_panel_mode)
    let leaderboard = Leaderboard(info_panel_mode)
    let patterns = Patterns(info_panel_mode)
    
    override this.Init(parent: Widget) =
        this
            .Add(
                scoreboard
                    .Conditional(fun () -> info_panel_mode.Value = InfoPanelMode.Local),
                leaderboard
                    .Conditional(fun () -> info_panel_mode.Value = InfoPanelMode.Online),
                patterns
                    .Conditional(fun () -> info_panel_mode.Value = InfoPanelMode.Patterns)
            )
            
        base.Init(parent)
            
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        scoreboard.Hide()
        leaderboard.Hide()
        patterns.Hide()
        
    member this.Show() : unit =
        scoreboard.Show(info_panel_mode.Value = InfoPanelMode.Local)
        leaderboard.Show(info_panel_mode.Value = InfoPanelMode.Online)
        patterns.Show(info_panel_mode.Value = InfoPanelMode.Patterns)
        

module InfoPanel =
    let SHOW_POS = Position.ShrinkT(TOP_BAR_HEIGHT + 5.0f).SlicePercentL(INFO_SCREEN_SPLIT)
    let HIDDEN_POS = Position.ShrinkT(TOP_BAR_HEIGHT + 5.0f).SlicePercentL(INFO_SCREEN_SPLIT).TranslateX(-SCREEN_OFFSET)

type InfoPanel() =
    inherit SlideContainer(NodeType.None)

    let info_panel_mode = Setting.simple InfoPanelMode.Local
    
    let mutable save_data = None
    
    let refresh(info: LoadedChartInfo) =
        save_data <- Some info.SaveData
        
    let change_rate (change_rate_by: Rate) : unit =
        if Transitions.in_progress() then
            ()
        else
            SelectedChart.rate.Value <- SelectedChart.rate.Value + change_rate_by
            LevelSelect.refresh_details ()
        
    let main_display = MainDisplay(info_panel_mode)
    let gameplay_info = GameplayInfo()
    let gameplay_utils = GameplayUtils(change_rate)

    override this.Init(parent: Widget) =
        SelectedChart.on_chart_change_finished.Add refresh
        SelectedChart.on_chart_update_finished.Add refresh
        SelectedChart.if_loaded refresh

        this
            .Add(
                main_display
                    .Position(MainDisplay.SHOW_POS),

                gameplay_info
                    .Position(GameplayInfo.HIDDEN_POS),

                gameplay_utils
                    .Position(GameplayUtils.HIDDEN_POS)
            )

        base.Init(parent)

    override this.Draw() =
        let info_area = gameplay_info.Bounds // this.Bounds.SliceB(GameplayInfo.HEIGHT).TranslateY(-75.0f)
        let chart_description_texture =
            match save_data with
            | Some save_data when save_data.PersonalBests.ContainsKey Rulesets.current_hash ->
                Content.Texture "chart-description"
            | _ -> Content.Texture "chart-description-nopb"
        Render.tex_quad
            (info_area |> _.AsQuad)
            Color.White.AsQuad
            (Sprite.pick_texture (0,0) chart_description_texture)
        base.Draw()
        
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        main_display.Hide()
        gameplay_info.Hide()
        gameplay_utils.Hide()
        
    member this.Show() : unit =
        main_display.Show()
        gameplay_info.Show()
        gameplay_utils.Show()