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

type InfoPanel() =
    inherit Container(NodeType.None)

    let info_panel_mode = Setting.simple InfoPanelMode.Local
    
    let mutable save_data = None
    
    let refresh(info: LoadedChartInfo) =
        save_data <- Some info.SaveData

    override this.Init(parent: Widget) =
        SelectedChart.on_chart_change_finished.Add refresh
        SelectedChart.on_chart_update_finished.Add refresh
        SelectedChart.if_loaded refresh

        let change_rate (change_rate_by: Rate) : unit =
            if Transitions.in_progress() then
                ()
            else
                SelectedChart.rate.Value <- SelectedChart.rate.Value + change_rate_by
                LevelSelect.refresh_details ()

        let main_display =
            Container(NodeType.None)
                .Position(Position.ShrinkB(GameplayInfo.HEIGHT + AngledButton.HEIGHT + Style.PADDING))
                .With(
                    Scoreboard(info_panel_mode)
                        .Conditional(fun () -> info_panel_mode.Value = InfoPanelMode.Local),
                    Leaderboard(info_panel_mode)
                        .Conditional(fun () -> info_panel_mode.Value = InfoPanelMode.Online),
                    Patterns(info_panel_mode)
                        .Conditional(fun () -> info_panel_mode.Value = InfoPanelMode.Patterns)
                )

        this
            .Add(
                main_display,

                GameplayInfo()
                    .Position(Position.SliceB(InlaidButton.HEIGHT + 20.0f, GameplayInfo.HEIGHT)),

                InlaidButton(
                    sprintf "%s %s" Icons.EYE %"levelselect.preview", 
                    (fun () -> SelectedChart.when_loaded false <| fun info -> Preview(info, change_rate).Show()),
                    ButtonType.CustomSprite "preview-button",
                    (13.0f, 15.0f)
                )
                    .Hotkey("preview")
                    .Position(
                        Position
                            .SliceB(InlaidButton.HEIGHT + 20.0f)
                            .GridX(1, 3, 0.0f)
                    )
                    .Help(Help.Info("levelselect.preview", "preview")),

                ModSelect(change_rate)
                    .Position(
                        Position
                            .SliceB(InlaidButton.HEIGHT + 20.0f)
                            .GridX(2, 3, 0.0f)
                    )
                    .Help(Help.Info("levelselect.mods", "mods")),

                RulesetSwitcher(options.SelectedRuleset)
                    .Position(
                        Position 
                            .SliceB(InlaidButton.HEIGHT + 20.0f)
                            .GridX(3, 3, 0.0f)
                    )
                    .Help(Help.Info("levelselect.rulesets", "ruleset_switch"))
            )

        base.Init(parent)

    override this.Draw() =
        let info_area = this.Bounds.SliceB(GameplayInfo.HEIGHT).TranslateY(-75.0f)
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