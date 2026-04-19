namespace Interlude.Features.Score

open System.Linq
open Percyqaz.Common
open Percyqaz.Flux.Windowing
open Percyqaz.Flux.UI
open Percyqaz.Flux.Graphics
open Prelude
open Prelude.Gameplay.Scoring
open Prelude.Data.User
open Prelude.Data.User.Stats
open Interlude.Content
open Interlude.UI
open Interlude.Features.Gameplay
open Interlude.Features.Online
open Prelude.Skins.Themes.Theme

#nowarn "3370"

// todo: refactor to a score-results object that is optional (played_just_now can be implied by it being Some)
type ScoreScreen(score_info: ScoreInfo, results: ImprovementFlags * SessionXPGain option, played_just_now: bool) =
    inherit Screen()

    let pbs, xp_gain = results
    let personal_bests = ref pbs

    let grade =
        ref
        <| Grade.calculate_with_target score_info.Ruleset.Grades score_info.Accuracy

    let lamp =
        ref
        <| Lamp.calculate_with_target score_info.Ruleset.Lamps score_info.Scoring.JudgementCounts score_info.Scoring.ComboBreaks

    let stats = ref <| ScoreScreenStats.calculate score_info.Scoring GraphSettings.column_filter

    let previous_personal_bests =
        match SelectedChart.SAVE_DATA with
        | Some d when d.PersonalBests.ContainsKey Rulesets.current_hash ->
            Some d.PersonalBests.[Rulesets.current_hash]
        | _ -> None
        |> ref

    let graph = new ScoreGraph(score_info, stats)

    let refresh () =
        personal_bests := ImprovementFlags.None

        grade
        := Grade.calculate_with_target score_info.Ruleset.Grades score_info.Accuracy

        lamp
        := Lamp.calculate_with_target score_info.Ruleset.Lamps score_info.Scoring.JudgementCounts score_info.Scoring.ComboBreaks

        stats := ScoreScreenStats.calculate score_info.Scoring GraphSettings.column_filter
        previous_personal_bests := None
        graph.Refresh()

    let on_ruleset_changed = Rulesets.on_changed.Subscribe (fun _ -> GameThread.defer refresh)

    let bottom_info =
        BottomBanner(
            score_info,
            played_just_now,
            graph,
            refresh
        )
            .Position(Position.SlicePercentB(0.35f))
            
    member private this.DrawRank(bounds: Rect, data: string): unit =
        if TEXTURES.Contains(data.ToLower().Replace("+", "plus")) then
            let sprite = Content.Texture (data.ToLower().Replace("+", "plus"))
            let r = bounds.SliceX(400.0f).TranslateX(730.0f).SliceY(400.0f).TranslateY(-150.0f)
            
            Render.tex_quad
                (r |> _.AsQuad)
                Color.White.AsQuad
                (Sprite.pick_texture (0,0) sprite)

    override this.Init(parent: Widget) =
        this
        |+ GameDetails(
            grade,
            lamp,
            personal_bests,
            previous_personal_bests,
            stats,
            score_info
        )
            .Position(Position.SliceT(260.0f).TranslateY(165.0f).SliceL(410.0f))
        |+ PlayerRating(
            score_info
        )
            .Position(Position.SliceT(170.0f).SliceR(365.0f).TranslateY(660.0f).TranslateX(-70.0f))
        |+ TopBanner(score_info)
            .Position(Position.SliceT(180.0f))
        |+ MiddleBox(
            stats,
            score_info
        )
            .Position(Position.SliceT(580.0f).TranslateY(246.0f).SliceR(885.0f).TranslateX(-540.0f))
        |+ bottom_info
        |* Confetti()

        ScoreScreenHelpers.animation_queue.Add (Animation.Delay 1000.0)

        //match xp_gain with
        //| Some x ->
        //    SessionScoreBar(x)
        //        .Position(Position.SlicePercentR(0.65f).ShrinkT(395.0f).SliceT(40.0f).ShrinkX(40.0f))
        //    |> this.Add
        //| None -> ()

        //Sounds.get("score-screen").Play()

        base.Init parent

    override this.Update(elapsed_ms, moved) =
        ScoreScreenHelpers.animation_queue.Update elapsed_ms
        base.Update(elapsed_ms, moved)

    override this.OnEnter prev =
        Toolbar.hide ()
        DiscordRPC.in_menus ("Admiring a score")

    override this.OnExit next =
        score_info.Ruleset <- Rulesets.current
        (graph :> System.IDisposable).Dispose()
        on_ruleset_changed.Dispose()
        // Sounds.get("score-screen").Stop()
        Toolbar.show (true, false)

    override this.OnBack() =
        if Network.lobby.IsSome then
            Some ScreenType.Lobby
        else
            Some ScreenType.LevelSelect

    override this.Draw() =

        // Render.rect (this.Bounds.ShrinkT(175.0f).SliceT(160.0f).ShrinkT(5.0f)) Colors.shadow_2.O2
        // Render.rect (this.Bounds.ShrinkT(175.0f).ShrinkT(160.0f).SliceT(5.0f)) Colors.white

        // Render.rect (bottom_info.Bounds.ShrinkT 5.0f) (Palette.color (127, 0.5f, 0.0f))
        // Render.rect (bottom_info.Bounds.SliceT 5.0f) Colors.white.O2
        let grade_name = score_info.Ruleset.GradeName (!grade).Grade
        this.DrawRank(this.Bounds, grade_name)
        
        let tex = Content.Texture "score-screen"
        Render.tex_quad
            (this.Bounds |> _.AsQuad)
            Colors.white.AsQuad
            (Sprite.pick_texture (0,0) tex)

        base.Draw()