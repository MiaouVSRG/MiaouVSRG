namespace MiaouVSRG.UI

open System
open System.IO

open System.Linq
open Microsoft.Win32
open Percyqaz.Common
open Percyqaz.Flux.Audio
open Catnip
open Catnip.Data.User.Stats
open MiaouVSRG.Options
open MiaouVSRG.Content
open MiaouVSRG.Features.Gameplay
open MiaouVSRG.Features.MainMenu
open MiaouVSRG.Features.Mounts
open MiaouVSRG.Features.LevelSelect
open MiaouVSRG.Features.Multiplayer
open MiaouVSRG.Features.Printnip
open MiaouVSRG.Features.Toolbar
open MiaouVSRG.Features.Online
open MiaouVSRG.Features.Stats

open MiaouVSRG.Features.Import

module Startup =
    
    /// <summary>
    /// FOR WINDOWS USERS ONLY. <br/>
    /// This function registers the "miaou://" uri in the regedit of the user
    /// </summary>
    let register_miaoudirect_uri () =
        if not (Registry.CurrentUser.GetSubKeyNames().Contains(@"Software\Classes\miaou")) then
            let key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\miaou")

            key.SetValue("", "URL:Miaou Protocol")
            key.SetValue("URL Protocol", "")

            let cmd = key.CreateSubKey(@"shell\open\command")

            let exe = Environment.ProcessPath

            cmd.SetValue(
                "",
                sprintf "\"%s\" \"%%1\"" exe
            )

    let mutable private deinit_required = false
    let mutable private deinit_once = false

    let init (instance: int) : Screen.ScreenRoot =
        if OperatingSystem.IsWindows() then
            register_miaoudirect_uri()
        
        Options.init ()
        Content.init ()

        let post_init_thunk () =

            Content.load_data ()
            Printnip.init instance
            Stats.init Content.Library Content.UserData
            StatsSync.init ()
            SelectedChart.init ()
            Mounts.init ()
            Network.init ()
            DiscordRPC.init ()
            Updates.check_for_updates ()

            deinit_required <- true

        Screen.init
            [|
                LoadingScreen(post_init_thunk)
                MainMenuScreen()
                LobbyScreen()
                LevelSelectScreen()
            |]

        Audio.change_volume (options.AudioVolume.Value, options.AudioVolume.Value)
        Song.set_pitch_rates_enabled options.AudioPitchRates.Value

        Gameplay.watch_replay <- LevelSelect.watch_replay
        Gameplay.continue_endless_mode <- LevelSelect.continue_endless_mode
        Gameplay.retry <- fun () -> SelectedChart.if_loaded LevelSelect.play

        Screen.ScreenRoot(Toolbar())

    type ShutdownType =
        | Normal
        | InternalCrash
        | ExternalCrash

    let deinit (shutdown_type: ShutdownType) (show_crash_splash: unit -> unit) : unit =
        if deinit_once then
            ()
        else
            deinit_once <- true

            if deinit_required then
                Stats.save_current_session (Timestamp.now()) Content.UserData
                Content.deinit ()
                Options.deinit ()
                Network.deinit ()
                Printnip.deinit ()
                DiscordRPC.deinit ()

            match shutdown_type with
            | Normal -> Logging.Info "Thank you for playing"
            | InternalCrash ->
                show_crash_splash ()
                Logging.Shutdown()
                Option.iter open_directory Logging.LogFile
            | ExternalCrash ->
                show_crash_splash ()
                Logging.Critical "The game was abnormally force-quit, but was able to shut down correctly"
                Logging.Shutdown()