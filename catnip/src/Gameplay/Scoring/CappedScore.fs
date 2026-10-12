namespace Catnip.Gameplay.Scoring

open Catnip

module CappedScore =
    let calculate(points: float, max_score: float, max_possible_points: float, total_notes: int, rate: Rate) =
        let max_score =
            if rate < 1.0f<rate> then
                max_score * float rate
            else
                max_score
                
        let score = (max_score / float total_notes) * (points / max_possible_points)
        score