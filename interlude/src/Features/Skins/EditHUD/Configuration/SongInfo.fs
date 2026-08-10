namespace Interlude.Features.Skins.EditHUD

open Percyqaz.Common
open Percyqaz.Flux.UI
open Prelude
open Interlude.Content
open Interlude.UI

type SongInfoPage() =
    inherit Page()

    let config = Content.HUD
    let chart_title = Setting.simple config.SongInfoShowChartTitle
    let chart_difficulty = Setting.simple config.SongInfoShowChartDifficulty
    let chart_rating = Setting.simple config.SongInfoShowChartRating
    let chart_remaining_time = Setting.simple config.SongInfoShowChartRemainingTime
    let chart_background = Setting.simple config.SongInfoShowChartBackground
    let custom_background = Setting.simple config.SongInfoShowCustomBackground

    member this.SaveChanges() =
        Skins.save_hud_config
            { Content.HUD with
                SongInfoShowChartTitle = chart_title.Value
                SongInfoShowChartDifficulty = chart_difficulty.Value
                SongInfoShowChartRating = chart_rating.Value
                SongInfoShowChartRemainingTime = chart_remaining_time.Value
                SongInfoShowChartBackground = chart_background.Value
                SongInfoShowCustomBackground = custom_background.Value
            }

    override this.Content() =
        this.OnClose(this.SaveChanges)
        
        page_container()
            .With(
                PageSetting(%"hud.song_info.chart_title", Checkbox chart_title)
                    .Pos(0),
                PageSetting(%"hud.song_info.chart_difficulty", Checkbox chart_difficulty)
                    .Pos(2),
                PageSetting(%"hud.song_info.chart_rating", Checkbox chart_rating)
                    .Pos(4),
                PageSetting(%"hud.song_info.chart_remaining_time", Checkbox chart_remaining_time)
                    .Pos(6),
                PageSetting(%"hud.song_info.chart_background", Checkbox chart_background)
                    .Help(Help.Info("hud.song_info.chart_background"))
                    .Pos(8),
                PageSetting(%"hud.song_info.custom_background", Checkbox custom_background)
                    .Help(Help.Info("hud.song_info.custom_background"))
                    .Pos(10)
            )

    override this.Title = %"hud.song_info"