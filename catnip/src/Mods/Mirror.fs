namespace Catnip.Mods

open Catnip

module Mirror =

    let apply (chart: ModdedChartInternal) : ModdedChartInternal * bool =
        { chart with
            Notes = TimeArray.map Array.rev chart.Notes
        },
        true