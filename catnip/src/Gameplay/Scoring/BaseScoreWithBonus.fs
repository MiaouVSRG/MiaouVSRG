namespace Catnip.Gameplay.Scoring

open Catnip
open Percyqaz.Common

/// Scoring system based on osu! Score V1 system : https://osu.ppy.sh/wiki/en/Gameplay/Score/ScoreV1/osu%21mania
module BaseScoreWithBonus =
    let calculate(
        points: float,
        hit_bonus_value: float,
        hit_bonus: float,
        hit_punishment: float,
        max_score: float,
        total_notes: int,
        max_points_reachable: float,
        last_bonus_value: float, rate: Rate
    ) =
        let rate_multiplier =
            if rate < 1.0f<rate> then
                float rate
            else
                1.0

        let base_score =
            (max_score * rate_multiplier * 0.5 / float total_notes)
            * (points / max_points_reachable)

        let bonus_points =
            last_bonus_value + hit_bonus - hit_punishment

        let bonus_points_final =
            bonus_points |> max 0.0 |> min 100.0

        let bonus_score =
            (max_score * rate_multiplier * 0.5 / float total_notes)
            * (hit_bonus_value * sqrt bonus_points_final / 320.0)
        
        (base_score + bonus_score), bonus_points_final