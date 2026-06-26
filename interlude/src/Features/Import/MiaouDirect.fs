namespace Interlude.Features.Import

open System.IO
open System.Threading
open System.Threading.Tasks
open Percyqaz.Common
open Percyqaz.Flux.Windowing
open Prelude
open Prelude.Data.Library.Imports
open Interlude.UI
open Interlude.Content

module MiaouDirect =
    let download (hash: string) =
        let task_tracking = TaskTracking.add hash

        let task = OnlineImports.download_osu_set($"https://beta.api.miaouvsrg.com/v2/download?id={hash}", Content.Charts, Content.UserData, task_tracking.set_Progress)
        import_queue.Request(task,
            function
            | Ok result ->
                Notifications.task_feedback (
                    Icons.DOWNLOAD,
                    %"notification.install_song",
                    [hash; result.ConvertedCharts.ToString(); result.SkippedCharts.Length.ToString()] %> "notification.install_song.body"
                )
                Content.TriggerChartAdded()
            | Error reason ->
                Logging.Error "Error importing %s: %s" hash reason
                Notifications.error (%"notification.install_song_failed", hash)
        )
