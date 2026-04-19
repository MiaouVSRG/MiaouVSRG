namespace Interlude.Features.Score

open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Percyqaz.Flux.Input
open Prelude
open Prelude.Calculator.KeymodeSkillBreakdown
open Prelude.Gameplay.Scoring
open Prelude.Data.User
open Interlude.UI

#nowarn "3370"

type Grade(grade: GradeResult ref, score_info: ScoreInfo) =
    inherit Container(NodeType.None)

    override this.Init(parent) =
        this
        |* Text((fun () -> score_info.Ruleset.GradeName (!grade).Grade))
            .Color((fun () -> (score_info.Ruleset.GradeColor (!grade).Grade, Colors.black)))
            .Position(Position.Expand(10.0f))

        base.Init parent

    override this.Draw() =
        // Render.rect (this.Bounds.Translate(10.0f, 10.0f)) Colors.black
        // Background.draw (this.Bounds, (Color.FromArgb(40, 40, 40)), 2.0f)
        // let grade_color = score_info.Ruleset.GradeColor (!grade).Grade
        // Render.rect this.Bounds grade_color.O1
        base.Draw()

type Accuracy
    (
        grade: GradeResult ref,
        improvements: ImprovementFlags ref,
        previous_personal_bests: Bests option ref,
        stats: ScoreScreenStats ref,
        score_info: ScoreInfo
    ) =
    inherit StaticWidget(NodeType.None)

    let LOWER_SIZE = 40.0f
    let new_record = sprintf "%s %s" Icons.AWARD (%"score.new_record")
    let mutable hover = false

    let glint_animation = Animation.Delay(375.0)

    override this.Init(parent) =
        if (!improvements).Accuracy <> Improvement.None then ScoreScreenHelpers.animation_queue.Add glint_animation
        base.Init parent

    override this.Update(elapsed_ms, moved) =
        hover <- Mouse.hover this.Bounds
        base.Update(elapsed_ms, moved)

    override this.Draw() =
        let grade_color = score_info.Ruleset.GradeColor (!grade).Grade

        if (!stats).ColumnFilterApplied then
            Text.fill_b (
                Style.font,
                score_info.Scoring.Ruleset.FormatAccuracy (!stats).Accuracy,
                this.Bounds.Shrink(10.0f, 0.0f).ShrinkB(LOWER_SIZE),
                Colors.text_green,
                Alignment.CENTER
            )
        else
            Text.fill_b (
                Style.font,
                score_info.Scoring.FormattedAccuracy,
                this.Bounds.Shrink(10.0f, 0.0f).ShrinkB(LOWER_SIZE),
                (grade_color, Colors.black),
                Alignment.CENTER
            )

        let text, color =
            if (!stats).ColumnFilterApplied then
                let columns =
                    GraphSettings.column_filter
                    |> Seq.indexed
                    |> Seq.choose (fun (i, b) -> if b && i < score_info.WithMods.Keys then Some ((i + 1).ToString()) else None)
                    |> String.concat " "
                sprintf "%s: %s" %"score.graph.settings.column_filter" columns, Colors.text_green
            else

            match (!improvements).Accuracy with
            | Improvement.New -> new_record, Colors.text_yellow_2
            | Improvement.Faster r -> sprintf "%s  •  +%gx" new_record (System.MathF.Round(float32 r, 2)), Colors.text_cyan_2
            | Improvement.Better b -> sprintf "%s  •  +%.2f%%" new_record (b * 100.0), Colors.text_green_2
            | Improvement.FasterBetter(r, b) ->
                sprintf "%s  •  +%.2f%%  •  +%gx" new_record (b * 100.0) (System.MathF.Round(float32 r, 2)), Colors.text_pink_2
            | Improvement.None ->
                match (!previous_personal_bests) with
                | Some pbs ->
                    match PersonalBests.get_best_above score_info.Rate pbs.Accuracy with
                    | Some(v, r, _) ->

                        let summary, distance_from_pb =
                            if r > score_info.Rate then
                                sprintf "%s (%.2fx)" (score_info.Ruleset.FormatAccuracy v) r, (v - score_info.Scoring.Accuracy)
                            else
                                score_info.Ruleset.FormatAccuracy v, (v - score_info.Scoring.Accuracy)

                        if distance_from_pb < 0.0001 then
                            [summary] %> "score.your_record", (Colors.grey_2.O2, Colors.black)
                        else
                            [sprintf "%.2f%%" (distance_from_pb * 100.0); summary] %> "score.compare_accuracy",
                            (Colors.grey_2.O2, Colors.black)

                    | None -> "--", (Colors.grey_2.O2, Colors.black)
                | None -> "--", (Colors.grey_2.O2, Colors.black)

        Text.fill_b (Style.font, text, this.Bounds.Shrink(10.0f, 0.0f).SliceB(LOWER_SIZE), color, Alignment.CENTER)

        if hover then
            let acc_tooltip = this.Bounds.SliceX(150.0f).BorderB(60.0f).TranslateY(15.0f)
            Render.rect (acc_tooltip.Expand(Style.PADDING)) Colors.white
            Render.rect acc_tooltip Colors.shadow_2

            Text.fill_b (
                Style.font,
                sprintf "%.4f%%" (stats.Value.Accuracy * 100.0),
                acc_tooltip.Shrink(10.0f, 5.0f),
                (if stats.Value.ColumnFilterApplied then Colors.text_green else Colors.text),
                Alignment.CENTER
            )

type GameDetails
    (
        grade: GradeResult ref,
        lamp: LampResult ref,
        improvements: ImprovementFlags ref,
        previous_personal_bests: Bests option ref,
        stats: ScoreScreenStats ref, score_info: ScoreInfo
    ) =
    inherit Container(NodeType.None)

    override this.Init(parent) =
        this
        |+ Text(fun () -> sprintf "%s" score_info.Ruleset.Name)
            .Align(Alignment.CENTER)
            .Position(Position.SliceL(120.0f).SliceT(70.0f).TranslateY(55.0f).TranslateX(74.0f).Shrink(10.0f))
        |+ Text(fun () -> $"{score_info.Rate}x")
            .Align(Alignment.CENTER)
            .Position(Position.SliceL(120.0f).SliceT(70.0f).TranslateY(55.0f).TranslateX(215.0f).Shrink(10.0f))
        |* Text(fun () -> sprintf "%s" (if score_info.ModString() = "" then "NM" else score_info.ModString()))
            .Align(Alignment.CENTER)
            .Position(Position.SlicePercentX(1.0f).SliceT(70.0f).TranslateY(175.0f).Shrink(10.0f).TranslateX(-10.0f))

        base.Init parent