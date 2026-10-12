namespace MiaouVSRG.Features.Score

open Percyqaz.Flux.UI
open Catnip.Data.User

type ScoreDisplay(score_info: ScoreInfo) =
    inherit Container(NodeType.None)

    override this.Init(parent: Widget) =
        this
            .Add(
                Text(fun () -> sprintf "%i" (int score_info.Scoring.PointsScored))
            )
        base.Init(parent)