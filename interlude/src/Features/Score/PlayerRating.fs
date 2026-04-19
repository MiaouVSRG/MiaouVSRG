namespace Interlude.Features.Score

open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Prelude.Data.User

type PlayerRating
    (
        score_info: ScoreInfo
    ) =
    inherit Container(NodeType.None)

    override this.Init(parent) =
        this
            .Add(
                Text(sprintf "%.2f" score_info.Performance)
                    .Align(Alignment.CENTER)
                    .Position(Position.SlicePercentX(1.0f).SliceB(70.0f).TranslateY(-20.0f).Expand(10.0f))
            )

        base.Init parent
