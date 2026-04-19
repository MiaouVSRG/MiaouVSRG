namespace Interlude.Features.Toolbar

open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Audio
open Percyqaz.Flux.Input
open Percyqaz.Flux.UI
open Prelude
open Prelude.Data.User.Stats
open Interlude.Content
open Interlude.UI
open Interlude.Options
open Interlude.Features.Import
open Interlude.Features.Gameplay
open Interlude.Features.Stats
open Interlude.Features.OptionsMenu
open Interlude.Features.OptionsMenu.Library
open Interlude.Features.Printerlude
open Interlude.Features.Online

type Toolbar() =
    inherit Widget(NodeType.None)

    let HEIGHT = Toolbar.HEIGHT
    let BUTTON_WIDTH = 180.0f

    let mutable collapsed_by_user = false

    let container = Container(NodeType.None)
    let bottom_only_container = Container(NodeType.None)
    let top_only_container = Container(NodeType.None)

    let volume_when_hidden =
        VolumeSlider()
            .Position(Position.ShrinkY(HEIGHT))

    let load_preset (i: int) : unit =
        match Presets.load i with
        | Some success_name ->
            SelectedChart.recolor ()
            Notifications.action_feedback (Icons.ALERT_OCTAGON, %"notification.preset_loaded", success_name)
        | None -> ()

    let draw_waveform (bounds: Rect) : unit =
        let s = bounds.Width / 48.0f

        for i in 0..47 do
            let level =
                System.Math.Min((Audio.waveform.[i] + 0.01f) * Toolbar.slideout_amount.Value * 0.4f, HEIGHT)

            if Toolbar.draw_top then
                Render.rect
                    (Rect.FromSize(bounds.Left + float32 i * s + 2.5f, bounds.Top, s - 5.0f, level))
                    (Palette.color (int level + 70, 1.0f, 0.5f))

            if Toolbar.draw_bottom then
                Render.rect
                    (Rect.FromSize(bounds.Right - (float32 i + 1.0f) * s + 2.5f, bounds.Bottom - level, s - 5.0f, level))
                    (Palette.color (int level + 70, 1.0f, 0.5f))

    let import_status_fade = Animation.Fade 0.0f
    let import_button =
        InlaidButton(%"menu.import", (fun () -> ImportsPage().Show()), ButtonType.BottomRounded)
            .Icon(Icons.DOWNLOAD)
            .Hotkey("import")
            .With(
                TaskProgressMiniBar()
                    .Position(Position.BorderB(Style.PADDING))
            )

    override this.Init(parent: Widget) =
        bottom_only_container
            // Bottom right info
            .With(
                Text("Updates.version")
                    .Align(Alignment.RIGHT)
                    .Position(Position.SliceR(5.0f, 300.0f).SliceB(HEIGHT).SlicePercentT(0.5f)),

                Text(fun () -> System.DateTime.Now.ToString())
                    .Align(Alignment.RIGHT)
                    .Position(Position.SliceR(5.0f, 300.0f).SliceB(HEIGHT).SlicePercentB(0.5f))
            )
            // Bottom left back/jukebox
            .With(

                VolumeSlider()
                    .Position(Position.ShrinkY(HEIGHT))
            )
            // Hotkey behaviours when toolbar isn't hidden
            .Add(
                HotkeyListener("reload_content", fun () ->
                    if not (Dialog.exists()) then
                        Themes.reload_current ()
                        Skins.load()
                        Rulesets.load()
                        SelectedChart.recolor ()
                        Notifications.action_feedback (Icons.CHECK, %"notification.reload_content", "")
                ),

                HotkeyListener("preset1", fun () -> load_preset 1),
                HotkeyListener("preset2", fun () -> load_preset 2),
                HotkeyListener("preset3", fun () -> load_preset 3)
            )
        
        top_only_container
            .With(
                InlaidButton(%"menu.back", (fun () -> Screen.back Transitions.UnderLogo |> ignore), ButtonType.BottomRounded)
                    .Icon(Icons.ARROW_LEFT_CIRCLE)
                    .Position(Position.SliceT(InlaidButton.HEIGHT_BOTTOM_ROUNDED).SliceL(Style.PADDING * 2.0f, BUTTON_WIDTH).SliceY(InlaidButton.
                                                                                                                                        HEIGHT_BOTTOM_ROUNDED
                                  ))
                    .Help(Help.Info("menu.quick").Hotkey("quick_menu")),

                FlowContainer.LeftToRight<Widget>(BUTTON_WIDTH)
                    .Spacing(Style.PADDING * 2.0f)
                    .DisableNavigation()
                    .Position(Position.SliceT(InlaidButton.HEIGHT_BOTTOM_ROUNDED).ShrinkL(HEIGHT + Style.PADDING + BUTTON_WIDTH))
                    .With(
                        InlaidButton(%"menu.options", (fun () -> OptionsPage().Show()), ButtonType.BottomRounded)
                            .Icon(Icons.SETTINGS),

                        import_button,

                        InlaidButton(%"menu.stats", (fun () -> StatsPage().Show()), ButtonType.BottomRounded)
                            .Icon(Icons.TRENDING_UP)
                            .Hotkey("stats")

                        // InlaidButton(%"menu.wiki", WikiBrowserPage.Show)
                        //     .Icon(Icons.BOOK_OPEN)
                        //     .Hotkey("wiki")
                        //     .Conditional(fun () -> Screen.current_type = ScreenType.MainMenu)
                    ),
                    
                Jukebox()
                    .Position(Position.SliceT(HEIGHT - 20.0f).SlicePercentL(0.4f).ShrinkL(BUTTON_WIDTH + Style.PADDING * 4.0f).TranslateX(800.0f).SliceY(InlaidButton.
                                                                                                                                                             HEIGHT_BOTTOM_ROUNDED
                                  )),

                NetworkStatus()
                    .Position(Position.SliceT(HEIGHT).SliceR(300.0f))
            )
            // Hotkey behaviours when toolbar isn't hidden
            .Add(
                HotkeyListener("reload_content", fun () ->
                    if not (Dialog.exists()) then
                        Themes.reload_current ()
                        Skins.load()
                        Rulesets.load()
                        SelectedChart.recolor ()
                        Notifications.action_feedback (Icons.CHECK, %"notification.reload_content", "")
                ),

                HotkeyListener("preset1", fun () -> load_preset 1),
                HotkeyListener("preset2", fun () -> load_preset 2),
                HotkeyListener("preset3", fun () -> load_preset 3)
            )
        
        container
            // Bottom right info
            .With(
                Text("Updates.version")
                    .Align(Alignment.RIGHT)
                    .Position(Position.SliceR(5.0f, 300.0f).SliceB(HEIGHT).SlicePercentT(0.5f)),

                Text(fun () -> System.DateTime.Now.ToString())
                    .Align(Alignment.RIGHT)
                    .Position(Position.SliceR(5.0f, 300.0f).SliceB(HEIGHT).SlicePercentB(0.5f))
            )
            // Bottom left back/jukebox
            .With(

                VolumeSlider()
                    .Position(Position.ShrinkY(HEIGHT))
            )
            .With(
                InlaidButton(%"menu.back", (fun () -> Screen.back Transitions.UnderLogo |> ignore), ButtonType.BottomRounded)
                    .Icon(Icons.ARROW_LEFT_CIRCLE)
                    .Position(Position.SliceT(InlaidButton.HEIGHT_BOTTOM_ROUNDED).SliceL(Style.PADDING * 2.0f, BUTTON_WIDTH).SliceY(InlaidButton.
                                                                                                                                        HEIGHT_BOTTOM_ROUNDED
                                  ))
                    .Help(Help.Info("menu.quick").Hotkey("quick_menu")),

                FlowContainer.LeftToRight<Widget>(BUTTON_WIDTH)
                    .Spacing(Style.PADDING * 2.0f)
                    .DisableNavigation()
                    .Position(Position.SliceT(InlaidButton.HEIGHT_BOTTOM_ROUNDED).ShrinkL(HEIGHT + Style.PADDING + BUTTON_WIDTH))
                    .With(
                        InlaidButton(%"menu.options", (fun () -> OptionsPage().Show()), ButtonType.BottomRounded)
                            .Icon(Icons.SETTINGS),

                        import_button,

                        InlaidButton(%"menu.stats", (fun () -> StatsPage().Show()), ButtonType.BottomRounded)
                            .Icon(Icons.TRENDING_UP)
                            .Hotkey("stats")

                        // InlaidButton(%"menu.wiki", WikiBrowserPage.Show)
                        //     .Icon(Icons.BOOK_OPEN)
                        //     .Hotkey("wiki")
                        //     .Conditional(fun () -> Screen.current_type = ScreenType.MainMenu)
                    ),
                    
                Jukebox()
                    .Position(Position.SliceT(HEIGHT - 20.0f).SlicePercentL(0.4f).ShrinkL(BUTTON_WIDTH + Style.PADDING * 4.0f).TranslateX(800.0f).SliceY(InlaidButton.
                                                                                                                                                             HEIGHT_BOTTOM_ROUNDED
                                  )),

                NetworkStatus()
                    .Position(Position.SliceT(HEIGHT).SliceR(300.0f))
            )
            // Hotkey behaviours when toolbar isn't hidden
            .Add(
                HotkeyListener("reload_content", fun () ->
                    if not (Dialog.exists()) then
                        Themes.reload_current ()
                        Skins.load()
                        Rulesets.load()
                        SelectedChart.recolor ()
                        Notifications.action_feedback (Icons.CHECK, %"notification.reload_content", "")
                ),

                HotkeyListener("preset1", fun () -> load_preset 1),
                HotkeyListener("preset2", fun () -> load_preset 2),
                HotkeyListener("preset3", fun () -> load_preset 3)
            )

        base.Init(parent)

        this.Bounds <-
            if Toolbar.hidden then
                this.Parent.Bounds.Expand(0.0f, HEIGHT)
            else
                this.Parent.Bounds.Expand(0.0f, HEIGHT * (1.0f - Toolbar.slideout_amount.Value))

        this.VisibleBounds <-
            if Toolbar.hidden then
                this.Parent.Bounds
            else
                this.Parent.Bounds.Expand(0.0f, HEIGHT * 2.0f)

        volume_when_hidden.Init(this)
        bottom_only_container.Init(this)
        top_only_container.Init(this)

    override this.Draw() =
        if Toolbar.hidden || not Toolbar.draw_bottom then
            volume_when_hidden.Draw()
            
        if Toolbar.slideout_amount.Value > 0.01f then draw_waveform this.Bounds

        if Toolbar.draw_top then
            top_only_container.Draw()
            Render.rect (this.Bounds.SliceT HEIGHT) Color.Transparent
                
        if Toolbar.draw_bottom then
            bottom_only_container.Draw()
            Render.rect (this.Bounds.SliceB HEIGHT) Color.Transparent

        if import_status_fade.Value > 0.005f then
            TaskTracking.draw (this.Bounds.ShrinkY(HEIGHT).SlicePercentL(0.4f).Shrink(20.0f), import_status_fade.Value)

        Terminal.draw ()

    override this.Update(elapsed_ms, moved) =
        if Screen.current_type <> ScreenType.SplashScreen then
            CURRENT_SESSION.GameTime <- CURRENT_SESSION.GameTime + elapsed_ms

        Toolbar.slideout_amount.Update elapsed_ms

        let moved =
            if Toolbar.was_hidden <> Toolbar.hidden then
                Toolbar.was_hidden <- Toolbar.hidden
                true
            else
                moved || Toolbar.slideout_amount.Moving

        if not Toolbar.hidden && (%%"toolbar").Pressed() then
            collapsed_by_user <- not collapsed_by_user
            Toolbar.slideout_amount.Target <- if collapsed_by_user then 0.0f else 1.0f

        if Screen.current_type <> ScreenType.Score && (%%"screenshot").Pressed() then
            Toolbar.take_screenshot()

        if (Screen.current_type = ScreenType.Score || not Toolbar.hidden) && (%%"options").Pressed() then
            OptionsPage().Show()
        if (Screen.current_type = ScreenType.Score || not Toolbar.hidden) && (%%"quick_menu").Pressed() then
            QuickMenuPage().Show()

        if Mouse.hover(import_button.Bounds) && TaskTracking.in_progress() then
            import_status_fade.Target <- 1.0f
        else
            import_status_fade.Target <- 0.0f
        import_status_fade.Update elapsed_ms

        Terminal.update ()

        if moved then
            this.Bounds <-
                if Toolbar.hidden then
                    this.Parent.Bounds.Expand(0.0f, HEIGHT)
                else
                    this.Parent.Bounds.Expand(0.0f, HEIGHT * (1.0f - Toolbar.slideout_amount.Value))

            this.VisibleBounds <-
                if Toolbar.hidden then
                    this.Parent.Bounds
                else
                    this.Parent.Bounds.Expand(0.0f, HEIGHT * 2.0f)

        // The volume slider is shown only when the bottom is drawn
        if Toolbar.hidden || not Toolbar.draw_bottom then
            volume_when_hidden.Update(elapsed_ms, moved)
            
        if Toolbar.draw_bottom then
            bottom_only_container.Update(elapsed_ms, moved)
            
        if Toolbar.draw_top then
            top_only_container.Update(elapsed_ms, moved)

    override this.Position
        with set _ = failwith "Position cannot be set for toolbar"