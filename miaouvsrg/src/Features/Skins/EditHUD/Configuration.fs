namespace MiaouVSRG.Features.Skins.EditHUD

open Catnip.Skins.HudLayouts

[<AutoOpen>]
module private ConfigurationPageHelper =

    let show_menu (element: HudElement) (on_close: unit -> unit) : unit =
        match element with
        | HudElement.Accuracy -> AccuracyPage() :> MiaouVSRG.UI.Page
        | HudElement.ErrorBar -> ErrorBarPage()
        | HudElement.Combo -> ComboPage()
        | HudElement.SkipButton -> SkipButtonPage()
        | HudElement.Judgement -> JudgementPage()
        | HudElement.EarlyLate -> EarlyLatePage()
        | HudElement.ProgressPie -> ProgressPiePage()
        | HudElement.JudgementCounter -> JudgementCounterPage()
        | HudElement.RateMods -> RateModsPage()
        | HudElement.BPM -> BPMPage()
        | HudElement.InputMeter -> InputMeterPage()
        | HudElement.Pacemaker -> PacemakerPage()
        | HudElement.KeysPerSecond -> KeysPerSecondPage()
        | HudElement.CustomImage -> CustomImagePage()
        | HudElement.SongInfo -> SongInfoPage()
        |> fun page -> page.WithOnClose(on_close).Show()