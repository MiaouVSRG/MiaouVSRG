namespace Prelude.Gameplay.Rulesets

open Prelude.Charts

[<AutoOpen>]
module DefaultRulesets =

    // load all default rulesets in Defaults.fs
    let EASY = Defaults.EASY
    let EASY_HASH = Ruleset.hash EASY

    let NORMAL = Defaults.NORMAL
    let NORMAL_HASH = Ruleset.hash NORMAL

    let HARD = Defaults.HARD
    let HARD_HASH = Ruleset.hash HARD

    let INSANE = Defaults.STRICT
    let INSANE_HASH = Ruleset.hash INSANE

    // let a default ruleset, that will be used everywhere in code : 
    // - Performance calculation
    // - Default judgement on first installation
    // - Leaderboards
    // - User data
    let DEFAULT_RULESET = NORMAL
    let DEFAULT_RULESET_HASH = NORMAL_HASH

module Rulesets =

    let get_native_ruleset (origins: ChartOrigin seq) : Ruleset option =
        let mutable wife3_as_fallback = false
        match
            Seq.tryPick
                (
                    function
                    | ChartOrigin.Etterna _ -> wife3_as_fallback <- true; None
                    | ChartOrigin.Osu osu -> Some (OsuMania.create osu.SourceOD OsuMania.NoMod)
                    | ChartOrigin.Quaver _ -> Some (Quaver.create Quaver.Standard)
                )
                origins
        with
        | None when wife3_as_fallback -> Some (Wife3.create 4)
        | otherwise -> otherwise