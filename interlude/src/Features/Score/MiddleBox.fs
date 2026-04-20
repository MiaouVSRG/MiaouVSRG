namespace Interlude.Features.Score

open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Prelude.Mods
open Prelude.Gameplay.Scoring
open Prelude.Data.User
open Prelude.Calculator
open Interlude.UI

#nowarn "3370"

type MiddleBox(stats: ScoreScreenStats ref, score_info: ScoreInfo) =
    inherit Container(NodeType.None)

    let show_more_info = Setting.simple false

    let mod_string = ModState.format (score_info.Rate, score_info.Mods)

    let category, main_clusters =
        let c = score_info.ChartMeta.Patterns
        c.Category,
        c.ImportantClusters |> Seq.truncate 3 |> Seq.map (fun c -> c.Format score_info.Rate) |> String.concat ", "

    let keymode_label =
        if score_info.WithMods.Keys <> score_info.Chart.Keys then
            sprintf "%iK->%iK" score_info.Chart.Keys score_info.WithMods.Keys
        else
            sprintf "%iK" score_info.WithMods.Keys

    override this.Init(parent) =
        this
        |+ Text(fun () -> sprintf "%s" (score_info.Scoring.Ruleset.FormatAccuracy (!stats).Accuracy))
            .Align(Alignment.CENTER)
            .Position(Position.SliceB(70.0f).SliceR(160.0f).TranslateX(-115.0f).TranslateY(-20.0f).Expand(10.0f))
        |* Text(fun () -> sprintf "%ix" score_info.Scoring.BestCombo)
            .Align(Alignment.CENTER)
            .Position(Position.SliceB(70.0f).SliceL(110.0f).TranslateX(110.0f).TranslateY(-20.0f).Expand(10.0f))

        base.Init(parent)

    override this.Draw() =
        // Render.rect_c this.Bounds (Quad.gradient_top_to_bottom (!*Palette.DARKER.O3) Colors.shadow_2.O3)
        base.Draw()

        // accuracy info
        let counters = this.Bounds.ShrinkX(25.0f).SliceT(170.0f, 350.0f)

        let judgement_counts = (!stats).Judgements
        let judgements = score_info.Ruleset.Judgements |> Array.indexed
        let h = counters.Height / float32 judgements.Length
        let mutable y = 0.0f
        
        let mutable column_position = 0.0f

        for i, j in judgements do
            let percentage_of_total = if (!stats).JudgementCount = 0 then 0.0f else float32 judgement_counts.[i] / float32 (!stats).JudgementCount

            let judgement_box = counters.SliceT(y, h) 

            let padding = Style.PADDING + 6.0f - float32 judgement_counts.Length |> max 0.0f
            
            let will_wrap = i % 2 = 0
            
            if will_wrap then column_position <- column_position + 1.0f

            Text.draw_b ( 
                Style.font,
                sprintf "%i" judgement_counts.[i],
                60.0f,
                (if i = 3 || i = 0 then this.Bounds.Right - 220.0f else this.Bounds.Left + 230.0f),
                this.Bounds.Top - 50.0f + 130.0f * column_position,
                Colors.text
            )

            if show_more_info.Value && i > 0 then
                let ratio =
                    if judgement_counts.[i] = 0 then sprintf "%i : 0" judgement_counts.[i - 1]
                    else sprintf "%.1f : 1" (float32 judgement_counts.[i - 1] / float32 judgement_counts.[i])

                Text.fill_b (
                    Style.font,
                    ratio,
                    judgement_box.TranslateY(-h * 0.5f).Shrink(Style.PADDING * 2.0f, Style.PADDING + padding * 2.0f),
                    (if (!stats).ColumnFilterApplied then (Colors.green_accent.O3, Colors.green_shadow) else (Colors.white.O3, Colors.shadow_2)),
                    Alignment.RIGHT
                )

            y <- y + h