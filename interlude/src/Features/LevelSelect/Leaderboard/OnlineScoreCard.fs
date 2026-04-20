namespace Interlude.Features.LevelSelect

open Interlude.Content
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Prelude.Data.User
open Interlude.UI
open Interlude.Features.Score

type private OnlineScoreCard(score: LeaderboardScore, score_info: ScoreInfo) =
    inherit
        Container(
            NodeType.Button(
                (fun () ->
                    Screen.change_new
                        (fun () -> new ScoreScreen(score_info, (ImprovementFlags.None, None), false) :> Screen)
                        ScreenType.Score
                        Transitions.EnterGameplayNoFadeAudio
                    |> ignore
                )
            )
        )

    let fade = Animation.Fade(0.0f, Target = 1.0f)
    let animation = Animation.seq [ Animation.Delay 150; fade ]

    override this.Init(parent: Widget) =
        let text_color =
            fun () -> let a = fade.Alpha in (Colors.white.O4a a, Colors.shadow_1.O4a a)

        let text_subcolor =
            fun () -> let a = fade.Alpha in (Colors.grey_1.O4a a, Colors.shadow_2.O4a a)
            
        let show_mod_string =
            if not (score_info.ModString() = "") then
                $" • {score_info.ModString()}"
            else
                ""

        this
            .Add(
                Text(fun () ->
                    sprintf "#%i %s"
                        score.Rank
                        score.Username
                )
                    .Color(text_color)
                    .Align(Alignment.LEFT)
                    .Position(Position.SlicePercentT(0.6f).SlicePercentL(0.6f).TranslateY(5.0f).ShrinkL(10.0f)),

                Text(fun () ->
                    sprintf
                        "%s • %ix • %.2f %s"
                        (score_info.Ruleset.LampName score_info.Lamp)
                        score_info.Scoring.BestCombo
                        score_info.Performance
                        show_mod_string
                )
                    .Color(text_subcolor)
                    .Align(Alignment.LEFT)
                    .Position(Position.SlicePercentB(0.4f).SlicePercentL(0.5f).ExpandT(5.0f).TranslateY(5.0f).ShrinkL(10.0f)),

                Text(fun () ->
                    sprintf
                        "%.2fx"
                        score_info.Rate
                )
                    .Color(text_subcolor)
                    .Align(Alignment.RIGHT)
                    .Position(Position.SlicePercentB(0.4f).SlicePercentR(0.5f).ShrinkR(27.0f).ExpandT(5.0f).ShrinkB(2.0f).TranslateY(5.0f)),

                Text( fun () ->
                    sprintf
                        "%s"
                        score_info.Scoring.FormattedAccuracy
                )
                    .Color(text_color)
                    .Align(Alignment.RIGHT)
                    .Position(Position.SlicePercentT(0.6f).SlicePercentR(0.4f).ShrinkR(27.0f).Shrink(5.0f).TranslateY(3.0f)),

                MouseListener()
                    .Button(this)
                    .OnRightClick(fun () -> ScoreContextMenu(true, score_info).Show())
            )

        base.Init(parent)

    member this.Data = score_info

    member this.FadeOut() = fade.Target <- 0.0f

    override this.OnFocus(by_mouse: bool) =
        base.OnFocus by_mouse
        Style.hover.Play()
        
    override this.Draw() =
        
        let sprite =
            if this.Focused then
                Content.Texture "leaderboard-score-hover"
            else
                Content.Texture "leaderboard-score"
        let q = this.Bounds.ExpandB(10.0f).ExpandL(5.0F) |> _.AsQuad
        
        Render.tex_quad
            q
            Colors.white.AsQuad
            (Sprite.pick_texture (0,0) sprite)
        
        base.Draw()

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        animation.Update elapsed_ms

        if this.Focused && (not this.FocusedByMouse || Mouse.hover this.Bounds) then

            if (%%"context_menu").Pressed() then
                ScoreContextMenu(true, score_info).Show()

        elif this.Focused && (%%"select").Pressed() then

            if this.FocusedByMouse then
                LevelSelect.choose_this_chart()
            else
                Screen.change_new
                        (fun () -> new ScoreScreen(score_info, (ImprovementFlags.None, None), false) :> Screen)
                        ScreenType.Score
                        Transitions.EnterGameplayNoFadeAudio
                    |> ignore