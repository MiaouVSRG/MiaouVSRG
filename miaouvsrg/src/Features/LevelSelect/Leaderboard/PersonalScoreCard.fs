namespace MiaouVSRG.Features.LevelSelect

open System.Globalization
open System.Linq
open Catnip
open Catnip.Skins.Themes.Theme
open MiaouVSRG.Content
open MiaouVSRG.Features.Online
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Catnip.Data.User
open MiaouVSRG.UI
open MiaouVSRG.Features.Score

module PersonalScoreCard =
    let SHOW_POS = Position.SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 120.0f, 75.0f).SlicePercentL(INFO_SCREEN_SPLIT).SlicePercentL(0.75f).SliceX(500.0f)
    let HIDDEN_POS = Position.SliceB(InlaidButton.HEIGHT_BOTTOM_ROUNDED + 120.0f, 75.0f).SlicePercentL(0.75f).SliceX(500.0f).TranslateX(-SCREEN_OFFSET)

type private PersonalScoreCard(base_score: PersonalLeaderboardScore, base_score_info: ScoreInfo option) as this =
    
    inherit
        SlideContainer(
            NodeType.Button(
                (fun () ->
                    Screen.change_new
                        (fun () -> new ScoreScreen(this.ScoreInfo, (ImprovementFlags.None, None), false) :> Screen)
                        ScreenType.Score
                        Transitions.EnterGameplayNoFadeAudio
                    |> ignore
                )
            )
        )
        
    let mutable score = base_score
    let mutable score_info = base_score_info 
    
    let fade = Animation.Fade(0.0f, Target = 1.0f)
    let animation = Animation.seq [ Animation.Delay 150; fade ]
        
    member this.Score 
        with get() = score
        and set(value) = score <- value

    member this.ScoreInfo 
        with get() = score_info.Value
        and set(value: ScoreInfo) = score_info <- Some value

    override this.Init(parent: Widget) =
        let text_color =
            fun () -> let a = fade.Alpha in (Colors.white.O4a a, Colors.shadow_1.O4a a)

        let text_subcolor =
            fun () -> let a = fade.Alpha in (Colors.grey_1.O4a a, Colors.shadow_2.O4a a)

        this
            .Add(
                ProfilePicture(Network.credentials.Username)
                    .Position(Position.ShrinkL(10.0f).ShrinkT(10.0f).SliceL(65.0f)),
                Text(fun () ->
                    sprintf "%s"
                        ((int this.ScoreInfo.Scoring.PointsScored).ToString("N0", CultureInfo.InvariantCulture))
                )
                    .Color(text_color)
                    .Align(Alignment.LEFT)
                    .Position(Position.SlicePercentT(0.6f).SlicePercentL(0.6f).TranslateY(5.0f).ShrinkL(10.0f).TranslateX(130.0f)),

                Text(fun () ->
                    sprintf
                        "#%i"
                        (score.Rank)
                )
                    .Color(text_subcolor)
                    .Align(Alignment.LEFT)
                    .Position(Position.SlicePercentB(0.4f).SlicePercentL(0.5f).ExpandT(5.0f).TranslateY(5.0f).ShrinkL(10.0f).TranslateX(130.0f)),

                Text(fun () ->
                    sprintf
                        "%.2fx%s"
                        this.ScoreInfo.Rate
                        (if score_info.IsSome && not (score_info.Value.ModString() = "") then
                            $", {score_info.Value.ModString()}"
                        else
                            "")
                )
                    .Color(text_subcolor)
                    .Align(Alignment.RIGHT)
                    .Position(Position.SlicePercentB(0.4f).SlicePercentR(0.5f).ShrinkR(27.0f).ExpandT(5.0f).ShrinkB(2.0f).TranslateY(5.0f)),

                Text( fun () ->
                    sprintf
                        "%s - %s"
                        (if this.ScoreInfo.Ruleset.LampName this.ScoreInfo.Lamp = "FC" then "FC" else (string this.ScoreInfo.Scoring.BestCombo) + "x")
                        this.ScoreInfo.Scoring.FormattedAccuracy
                )
                    .Color(text_color)
                    .Align(Alignment.RIGHT)
                    .Position(Position.SlicePercentT(0.6f).SlicePercentR(0.4f).ShrinkR(27.0f).Shrink(5.0f).TranslateY(3.0f)),

                MouseListener()
                    .Button(this)
                    .OnRightClick(fun () -> ScoreContextMenu(true, this.ScoreInfo).Show())
            )

        base.Init(parent)

    member this.Data = score_info

    member this.FadeOut() = fade.Target <- 0.0f

    override this.OnFocus(by_mouse: bool) =
        base.OnFocus by_mouse
        Style.hover.Play()
        
    member private this.DrawGrade(bounds: Rect, grade: string, pos: float32): unit =
        if TEXTURES.Contains(grade.ToLower().Replace("+", "plus")) then
            let sprite = Content.Texture (grade.ToLower().Replace("+", "plus"))
            let r = bounds.SliceL(50.0f).SliceY(50.0f).TranslateX(pos)
            
            Render.tex_quad
                (r |> _.AsQuad)
                Color.White.AsQuad
                (Sprite.pick_texture (0,0) sprite)
        else
            Text.draw_aligned_b (
                Style.font,
                grade,
                20.0f,
                bounds.Right - pos,
                bounds.Top + 20.0f,
                (Colors.white, Color.Black),
                0.5f
            )
        
    override this.Draw() =
        
        let get_grade_name(acc: float) =
            if acc = 1.0 then
                "PERFECT"
            elif acc < 1.0 && acc >= 0.99 then
                "OVERCLEAR+"
            elif acc < 0.99 && acc >= 0.97 then
                "OVERCLEAR"
            elif acc < 0.97 && acc >= 0.95 then
                "CLEAR+"
            elif acc < 0.95 && acc >= 0.9 then
                "CLEAR"
            elif acc < 0.9 && acc >= 0.85 then
                "PASS"
            else
                "FAIL"
        
        let sprite =
            if this.Focused then
                Content.Texture "leaderboard-score-hover"
            else
                Content.Texture "leaderboard-score"
        let r = this.Bounds.ExpandB(10.0f)
        let q = r |> _.AsQuad
        
        Render.tex_quad
            q
            Colors.white.AsQuad
            (Sprite.pick_texture (0,0) sprite)
        
        if score_info.IsSome then    
            let grade_name = get_grade_name(score_info.Value.Accuracy)
            this.DrawGrade(r, grade_name, 85.0f)
        
        base.Draw()

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        animation.Update elapsed_ms

        if this.Focused && (not this.FocusedByMouse || Mouse.hover this.Bounds) then

            if (%%"context_menu").Pressed() then
                ScoreContextMenu(true, this.ScoreInfo).Show()

        elif this.Focused && (%%"select").Pressed() then

            if this.FocusedByMouse then
                LevelSelect.choose_this_chart()
            else
                Screen.change_new
                        (fun () -> new ScoreScreen(this.ScoreInfo, (ImprovementFlags.None, None), false) :> Screen)
                        ScreenType.Score
                        Transitions.EnterGameplayNoFadeAudio
                    |> ignore
                    
    member this.Hide(?snap: bool) =
        this.Position <- PersonalScoreCard.HIDDEN_POS
        if snap.IsSome && snap.Value then this.SnapPosition()
        
    member this.Show(?snap: bool) =
        this.Position <- PersonalScoreCard.SHOW_POS
        if snap.IsSome && snap.Value then this.SnapPosition()