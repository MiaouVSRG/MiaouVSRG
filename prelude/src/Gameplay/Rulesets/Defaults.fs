namespace Prelude.Gameplay.Rulesets

open System.Diagnostics.Tracing
open Prelude

module Defaults = 
    let EASY = 
        let MARV_TIMING : GameplayTime = 16.5f<ms / rate>
        let PERFECT_TIMING : GameplayTime = 49.5f<ms / rate>
        let GREAT_TIMING : GameplayTime = 82.5f<ms / rate>
        let MISS_TIMING : GameplayTime = 173.5f<ms / rate>
        {
            Name = sprintf "EASY"
            Description = "EASY Scoring System based on osu!mania OD5 judgements"
            Judgements =
                [|
                    {
                        Name = "300g"
                        Color = Color.FromArgb(0, 255, 255)
                        TimingWindows = Some (-MARV_TIMING, MARV_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "300"
                        Color = Color.FromArgb(255, 255, 0)
                        TimingWindows = Some (-PERFECT_TIMING, PERFECT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "200"
                        Color = Color.FromArgb(0, 255, 100)
                        TimingWindows = Some (-GREAT_TIMING, GREAT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "MISS"
                        Color = Color.FromArgb(255, 80, 80)
                        TimingWindows = Some (-MISS_TIMING, MISS_TIMING)
                        BreaksCombo = true
                    }
                |]
            Grades =
                [|
                    {
                        Name = "FAIL"
                        Accuracy = 0.0
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "FAIL"
                        Accuracy = 0.7
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "PASS"
                        Accuracy = 0.85
                        Color = Color.FromArgb(182, 96, 0)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR"
                        Accuracy = 0.9
                        Color = Color.FromArgb(0, 255, 100)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR+"
                        Accuracy = 0.95
                        Color = Color.FromArgb(246, 234, 128)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR"
                        Accuracy = 0.97
                        Color = Color.FromArgb(255, 108, 255)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR+"
                        Accuracy = 0.99
                        Color = Color.FromArgb(102, 255, 255)
                        TextureName = None
                    }
                    {
                        Name = "PERFECT"
                        Accuracy = 1
                        Color = Color.FromArgb(255, 255, 255)
                        TextureName = None
                    }
                |]
            Lamps =
                [|
                    {
                        Name = "FC"
                        Requirement = LampRequirement.ComboBreaksAtMost 0
                        Color = Color.FromArgb(0, 255, 160)
                    }
                |]
            Accuracy = AccuracyPoints.PointsPerJudgement([| 1.0; 1.0; 2.0/3.0; 2.0/9.0 |])
            HitMechanics =
                {
                    NotePriority = NotePriority.OsuMania
                    GhostTapJudgement = None
                }
            HoldMechanics = HoldMechanics.CombineHeadAndTail (HeadTailCombineRule.HeadJudgementOr (-173.5f<ms / rate>, 173.5f<ms / rate>, 2, 2))
            Formatting = { DecimalPlaces = DecimalPlaces.TWO }
        }

    let NORMAL = 
        let MARV_TIMING : GameplayTime = 16.5f<ms / rate>
        let PERFECT_TIMING : GameplayTime = 41.5f<ms / rate>
        let GREAT_TIMING : GameplayTime = 74.5f<ms / rate>
        let MISS_TIMING : GameplayTime = 165.5f<ms / rate>
        {
            Name = sprintf "NORMAL"
            Description = "NORMAL Scoring System based on osu!mania OD7.5 judgements"
            Judgements =
                [|
                    {
                        Name = "300g"
                        Color = Color.FromArgb(0, 255, 255)
                        TimingWindows = Some (-MARV_TIMING, MARV_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "300"
                        Color = Color.FromArgb(255, 255, 0)
                        TimingWindows = Some (-PERFECT_TIMING, PERFECT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "200"
                        Color = Color.FromArgb(0, 255, 100)
                        TimingWindows = Some (-GREAT_TIMING, GREAT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "MISS"
                        Color = Color.FromArgb(255, 80, 80)
                        TimingWindows = Some (-MISS_TIMING, MISS_TIMING)
                        BreaksCombo = true
                    }
                |]
            Grades =
                [|
                    {
                        Name = "FAIL"
                        Accuracy = 0.0
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "FAIL"
                        Accuracy = 0.7
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "PASS"
                        Accuracy = 0.85
                        Color = Color.FromArgb(182, 96, 0)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR"
                        Accuracy = 0.9
                        Color = Color.FromArgb(0, 255, 100)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR+"
                        Accuracy = 0.95
                        Color = Color.FromArgb(246, 234, 128)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR"
                        Accuracy = 0.97
                        Color = Color.FromArgb(255, 108, 255)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR+"
                        Accuracy = 0.99
                        Color = Color.FromArgb(102, 255, 255)
                        TextureName = None
                    }
                    {
                        Name = "PERFECT"
                        Accuracy = 1
                        Color = Color.FromArgb(255, 255, 255)
                        TextureName = None
                    }
                |]
            Lamps =
                [|
                    {
                        Name = "FC"
                        Requirement = LampRequirement.ComboBreaksAtMost 0
                        Color = Color.FromArgb(0, 255, 160)
                    }
                |]
            Accuracy = AccuracyPoints.PointsPerJudgement([| 1.0; 1.0; 2.0/3.0; 2.0/9.0 |])
            HitMechanics =
                {
                    NotePriority = NotePriority.OsuMania
                    GhostTapJudgement = None
                }
            HoldMechanics = HoldMechanics.CombineHeadAndTail (HeadTailCombineRule.HeadJudgementOr (-165.5f<ms / rate>, 165.5f<ms / rate>, 3, 2))
            Formatting = { DecimalPlaces = DecimalPlaces.TWO }
        }

    let HARD = 
        let MARV_TIMING : GameplayTime = 16.5f<ms / rate>
        let PERFECT_TIMING : GameplayTime = 34.5f<ms / rate>
        let GREAT_TIMING : GameplayTime = 67.5f<ms / rate>
        let MISS_TIMING : GameplayTime = 158.5f<ms / rate>
        {
            Name = sprintf "HARD"
            Description = "HARD Scoring System based on osu!mania OD10 judgements"
            Judgements =
                [|
                    {
                        Name = "300g"
                        Color = Color.FromArgb(0, 255, 255)
                        TimingWindows = Some (-MARV_TIMING, MARV_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "300"
                        Color = Color.FromArgb(255, 255, 0)
                        TimingWindows = Some (-PERFECT_TIMING, PERFECT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "200"
                        Color = Color.FromArgb(0, 255, 100)
                        TimingWindows = Some (-GREAT_TIMING, GREAT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "MISS"
                        Color = Color.FromArgb(255, 80, 80)
                        TimingWindows = Some (-MISS_TIMING, MISS_TIMING)
                        BreaksCombo = true
                    }
                |]
            Grades =
                [|
                    {
                        Name = "FAIL"
                        Accuracy = 0.0
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "FAIL"
                        Accuracy = 0.7
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "PASS"
                        Accuracy = 0.85
                        Color = Color.FromArgb(182, 96, 0)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR"
                        Accuracy = 0.9
                        Color = Color.FromArgb(0, 255, 100)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR+"
                        Accuracy = 0.95
                        Color = Color.FromArgb(246, 234, 128)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR"
                        Accuracy = 0.97
                        Color = Color.FromArgb(255, 108, 255)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR+"
                        Accuracy = 0.99
                        Color = Color.FromArgb(102, 255, 255)
                        TextureName = None
                    }
                    {
                        Name = "PERFECT"
                        Accuracy = 1
                        Color = Color.FromArgb(255, 255, 255)
                        TextureName = None
                    }
                |]
            Lamps =
                [|
                    {
                        Name = "FC"
                        Requirement = LampRequirement.ComboBreaksAtMost 0
                        Color = Color.FromArgb(0, 255, 160)
                    }
                |]
            Accuracy = AccuracyPoints.PointsPerJudgement([| 1.0; 1.0; 2.0/3.0; 2.0/9.0 |])
            HitMechanics =
                {
                    NotePriority = NotePriority.OsuMania
                    GhostTapJudgement = None
                }
            HoldMechanics = HoldMechanics.CombineHeadAndTail (HeadTailCombineRule.HeadJudgementOr (-158.5f<ms / rate>, 158.5f<ms / rate>, 3, 3))
            Formatting = { DecimalPlaces = DecimalPlaces.TWO }
        }

    let STRICT = 
        let MARV_TIMING : GameplayTime = 11.5f<ms / rate>
        let PERFECT_TIMING : GameplayTime = 32.5f<ms / rate>
        let GREAT_TIMING : GameplayTime = 56.5f<ms / rate>
        let MISS_TIMING : GameplayTime = 121.5f<ms / rate>
        {
            Name = sprintf "STRICT"
            Description = "STRICT Scoring System based on osu!mania OD10 judgements"
            Judgements =
                [|
                    {
                        Name = "300g"
                        Color = Color.FromArgb(0, 255, 255)
                        TimingWindows = Some (-MARV_TIMING, MARV_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "300"
                        Color = Color.FromArgb(255, 255, 0)
                        TimingWindows = Some (-PERFECT_TIMING, PERFECT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "200"
                        Color = Color.FromArgb(0, 255, 100)
                        TimingWindows = Some (-GREAT_TIMING, GREAT_TIMING)
                        BreaksCombo = false
                    }
                    {
                        Name = "MISS"
                        Color = Color.FromArgb(255, 80, 80)
                        TimingWindows = Some (-MISS_TIMING, MISS_TIMING)
                        BreaksCombo = true
                    }
                |]
            Grades =
                [|
                    {
                        Name = "FAIL"
                        Accuracy = 0.0
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "FAIL"
                        Accuracy = 0.7
                        Color = Color.FromArgb(255, 0, 0)
                        TextureName = None
                    }
                    {
                        Name = "PASS"
                        Accuracy = 0.85
                        Color = Color.FromArgb(182, 96, 0)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR"
                        Accuracy = 0.9
                        Color = Color.FromArgb(0, 255, 100)
                        TextureName = None
                    }
                    {
                        Name = "CLEAR+"
                        Accuracy = 0.95
                        Color = Color.FromArgb(246, 234, 128)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR"
                        Accuracy = 0.97
                        Color = Color.FromArgb(255, 108, 255)
                        TextureName = None
                    }
                    {
                        Name = "OVERCLEAR+"
                        Accuracy = 0.99
                        Color = Color.FromArgb(102, 255, 255)
                        TextureName = None
                    }
                    {
                        Name = "PERFECT"
                        Accuracy = 1
                        Color = Color.FromArgb(255, 255, 255)
                        TextureName = None
                    }
                |]
            Lamps =
                [|
                    {
                        Name = "FC"
                        Requirement = LampRequirement.ComboBreaksAtMost 0
                        Color = Color.FromArgb(0, 255, 160)
                    }
                |]
            Accuracy = AccuracyPoints.PointsPerJudgement([| 1.0; 1.0; 2.0/3.0; 2.0/9.0 |])
            HitMechanics =
                {
                    NotePriority = NotePriority.OsuMania
                    GhostTapJudgement = None
                }
            HoldMechanics = HoldMechanics.CombineHeadAndTail (HeadTailCombineRule.HeadJudgementOr (-121.5f<ms / rate>, 121.5f<ms / rate>, 3, 3))
            Formatting = { DecimalPlaces = DecimalPlaces.TWO }
        }