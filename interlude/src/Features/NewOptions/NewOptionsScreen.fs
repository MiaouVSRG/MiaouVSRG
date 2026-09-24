namespace Interlude.Features.OptionsScreen

open Interlude.Content
open Interlude.Features.OptionsMenu.Gameplay
open Interlude.Features.OptionsMenu.SystemSettings
open Interlude.Features.Pacemaker
open Interlude.Features.Skins
open Interlude.Options
open Interlude.UI
open Percyqaz.Flux.Windowing
open Prelude
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Percyqaz.Common

[<RequireQualifiedAccess>]
type NewOptionsTab =
    | System
    | Gameplay
    | Library
    | Noteskins
    | SearchResults of content: Widget
    
type private SettingsWidgets() =
    
    static member ScrollSpeed() : PageSetting =
        PageSetting(%"gameplay.scrollspeed", Slider.Percent(Setting.uom options.ScrollSpeed))
            .With(
                Text(fun () ->
                    [
                        ((1080.0f - options.HitPosition.Value) / float32 options.ScrollSpeed.Value).ToString("F0")
                        (float32 options.ScrollSpeed.Value * 12.698412f).ToString("F1")
                        (float32 options.ScrollSpeed.Value * 33.9f / 2.38f).ToString("F1")
                        "C" + (60000.0f * float32 options.ScrollSpeed.Value / Content.NoteskinConfig.DefaultColumnWidth).ToString("F0")
                    ]
                    %> "gameplay.scrollspeed.info"
                )
                    .Align(Alignment.LEFT)
                    .Position(page_position(2, 1, PageWidth.Normal).ShrinkL(PAGE_LABEL_WIDTH))
            )
            .Help(Help.Info("gameplay.scrollspeed"))

    static member HitPosition() : PageSetting =
        PageSetting(%"gameplay.hitposition", Slider(options.HitPosition, Step = 1f))
            .Help(Help.Info("gameplay.hitposition"))

    static member Upscroll() : PageSetting =
        PageSetting(%"gameplay.upscroll", Checkbox options.Upscroll)
            .Help(Help.Info("gameplay.upscroll"))

    static member BackgroundDim() : PageSetting =
        PageSetting(%"gameplay.backgrounddim", Slider.Percent(options.BackgroundDim))
            .Help(Help.Info("gameplay.backgrounddim"))

    static member HoldToGiveUp() : PageSetting =
        PageSetting(%"gameplay.hold_to_give_up", Checkbox options.HoldToGiveUp)
            .Help(Help.Info("gameplay.hold_to_give_up"))

    static member HideHitNotes() : PageSetting =
        PageSetting(%"gameplay.hide_hit_notes", Checkbox options.VanishingNotes)
            .Help(Help.Info("gameplay.hide_hit_notes"))

    static member OnQuitOut() : PageSetting =
        PageSetting(%"gameplay.on_quit_out",
            SelectDropdown(
                [|
                    QuitOutBehaviour.SaveAndShow, %"gameplay.on_quit_out.save_and_show"
                    QuitOutBehaviour.Show, %"gameplay.on_quit_out.show"
                    QuitOutBehaviour.Ignore, %"gameplay.on_quit_out.ignore"
                |],
                options.QuitOutBehaviour
            )
        )
        
    static member Lanecover() : PageButton =
        PageButton(%"gameplay.lanecover", fun () -> LanecoverPage().Show())
            .Help(Help.Info("gameplay.lanecover"))

    static member Pacemaker() : PageButton =
        PageButton(%"gameplay.pacemaker", fun () -> PacemakerOptionsPage().Show())
            .Help(Help.Info("gameplay.pacemaker"))

    static member Keybinds() : PageButton =
        PageButton(%"gameplay.keybinds", fun () -> GameplayBindsPage().Show())
            .Help(Help.Info("gameplay.keybinds"))
            
    
    
    static member WindowMode() : PageSetting =
        PageSetting(
            %"system.windowmode",
            SelectDropdown(
                [|
                    WindowType.Windowed, %"system.windowmode.windowed"
                    WindowType.Borderless, %"system.windowmode.borderless"
                    WindowType.BorderlessNoTaskbar, %"system.windowmode.fullscreen_borderless"
                    WindowType.Fullscreen, %"system.windowmode.fullscreen"
                    WindowType.FullscreenLetterbox, %"system.windowmode.fullscreen_letterbox"
                |],
                config.WindowMode
                |> Setting.trigger window_mode_changed
                |> Setting.trigger (ignore >> config.Apply)
            )
        )

    static member WindowedResolution() : Conditional<PageSetting> =
        PageSetting(
            %"system.windowresolution",
            WindowedResolutionPicker.Create(config.WindowedResolution |> Setting.trigger (ignore >> config.Apply))
        )
            .Conditional(fun () -> config.WindowMode.Value = WindowType.Windowed)

    static member Monitor() : Conditional<PageSetting> =
        PageSetting(
            %"system.monitor",
            SelectDropdown(
                monitors |> Seq.map (fun m -> m.Id, m.FriendlyName) |> Array.ofSeq,
                config.Display
                |> Setting.trigger (fun _ -> select_fullscreen_size (); config.Apply())
            )
        )
            .Conditional(fun () -> config.WindowMode.Value <> WindowType.Windowed)

    static member VideoMode() : Conditional<PageSetting> =
        PageSetting(
            %"system.videomode",
            VideoModePicker.Create(
                config.FullscreenVideoMode
                |> Setting.trigger (ignore >> config.Apply)
            )
        )
            .Help(Help.Info("system.videomode"))
            .Conditional(fun () -> config.WindowMode.Value = WindowType.Fullscreen)

    static member LetterboxResolution() : Conditional<PageSetting> =
        PageSetting(
            %"system.letterbox_resolution",
            WindowedResolutionPicker.Create(config.WindowedResolution |> Setting.trigger (ignore >> config.Apply))
        )
            .Conditional(fun () -> config.WindowMode.Value = WindowType.FullscreenLetterbox)

    static member Performance() : PageButton =
        PageButton(
            %"system.performance",
            (fun () -> PerformanceSettingsPage().Show())
        )

    static member Hotkeys() : PageButton =
        PageButton(%"system.hotkeys", (fun () -> HotkeysPage().Show()))

    static member VisualOffset() : PageSetting =
        PageSetting(%"system.visualoffset", Slider(Setting.uom options.VisualOffset, Step = 1f))
            .Help(Help.Info("system.visualoffset"))
    
module NewOptionsTabContainer =
    let HIDDEN_POS = Position.Shrink(275.0f, 154.0f).ShrinkPercentL(0.25f).Shrink(Style.PADDING * 3.0f).TranslateY(Render.height() - 154.0f)
    let SHOW_POS = Position.Shrink(275.0f, 154.0f).ShrinkPercentL(0.25f).Shrink(Style.PADDING * 3.0f)
    
type private NewOptionsTabContainer(tab: NewOptionsTab) =
    inherit SlideContainer(NodeType.None)
            
    override this.Init(parent: Widget) =
        let nav_gameplay =
            NavigationContainer.Column()
                .WrapNavigation(false)
                .With(
                    SettingsWidgets.ScrollSpeed().Pos(0),
                    SettingsWidgets.HitPosition().Pos(3),
                    SettingsWidgets.Upscroll().Pos(6),
                    SettingsWidgets.BackgroundDim().Pos(9),
                    SettingsWidgets.HoldToGiveUp().Pos(12),
                    SettingsWidgets.HideHitNotes().Pos(15),
                    SettingsWidgets.OnQuitOut().Pos(18),
                    SettingsWidgets.Lanecover().Pos(21),
                    SettingsWidgets.Pacemaker().Pos(23),
                    SettingsWidgets.Keybinds().Pos(25)
                )
                
        let nav_system =
            NavigationContainer.Column()
                .WrapNavigation(false)
                .With(
                    SystemPage.WindowMode().Pos(0),
                    SettingsWidgets.WindowedResolution().Pos(2),
                    SettingsWidgets.Monitor().Pos(2),
                    SettingsWidgets.VideoMode().Pos(4),
                    SettingsWidgets.LetterboxResolution().Pos(4),

                    SettingsWidgets.Performance().Pos(7),
                    SettingsWidgets.Hotkeys().Pos(9),

                    PageButton(%"system.audio", fun () -> AudioPage().Show()).Pos(12),
                    SettingsWidgets.VisualOffset().Pos(14)
                )
                
        let nav_skinning =
            NavigationContainer.Column()
                .WrapNavigation(false)
                .With(
                    SystemPage.WindowMode().Pos(0),
                    SettingsWidgets.WindowedResolution().Pos(2),
                    SettingsWidgets.Monitor().Pos(2),
                    SettingsWidgets.VideoMode().Pos(4),
                    SettingsWidgets.LetterboxResolution().Pos(4),

                    SettingsWidgets.Performance().Pos(7),
                    SettingsWidgets.Hotkeys().Pos(9),

                    PageButton(%"system.audio", fun () -> AudioPage().Show()).Pos(12),
                    SettingsWidgets.VisualOffset().Pos(14)
                )
        
        this
            .WithConditional((tab = NewOptionsTab.Gameplay), nav_gameplay)
            .AddConditional((tab = NewOptionsTab.System), nav_system)
        base.Init(parent)
        
    member this.Show(snap: bool) =
        this.Position <- NewOptionsTabContainer.SHOW_POS
        if snap then this.SnapPosition()
    
    member this.Hide(snap: bool) =
        this.Position <- NewOptionsTabContainer.HIDDEN_POS
        if snap then this.SnapPosition()
    
type private NewOptionsItem(tab: NewOptionsTab, on_click: unit -> unit) =
    inherit Container(NodeType.Button(fun () ->
        Style.click.Play()
        on_click()
    ))
    
    override this.Init(parent: Widget) =
        this
            .With(
                Text(fun () ->
                    match tab with
                    | NewOptionsTab.Gameplay -> "Gameplay"
                    | NewOptionsTab.Library -> "Library"
                    | NewOptionsTab.Noteskins -> "Skinning"
                    | NewOptionsTab.System -> "System"
                    | _ -> ""
                ).Position(Position.Shrink(75.0f, 75.0f))
            )
            .Add(
                MouseListener().Button(this)
            )
        base.Init(parent)
        
    override this.Draw() =
        Render.rect (this.Bounds.SliceB(5.0f)) Color.Aquamarine
        base.Draw()

module NewOptionsList =
    let HIDDEN_POS = Position.Shrink(275.0f, 154.0f).SlicePercentL(0.25f).TranslateY(Render.height() - 154.0f)
    let SHOW_POS = Position.Shrink(275.0f, 154.0f).SlicePercentL(0.25f)
    
type private NewOptionsList(options_list: FlowContainer.Vertical<NewOptionsItem>) =
    inherit SlideContainer(NodeType.None)
    
    let scroll_container = ScrollContainer(options_list)
    
    override this.Init(parent: Widget) =
        this.Add(
            scroll_container
        )
        base.Init(parent)
        
    override this.Draw() =
        Render.rect (this.Bounds.SliceR(5.0f)) Color.Aquamarine
        base.Draw()
        
    member this.Show() =
        this.Position <- NewOptionsList.SHOW_POS
    
    member this.Hide() =
        this.Position <- NewOptionsList.HIDDEN_POS

type NewOptionsScreen() =
    inherit Screen()
    
    let keys = FlowContainer.Vertical<NewOptionsItem>(193.0f)
    
    let fade = Animation.Fade 0.0f
    let enter_sequence = Animation.Group()
    
    let mutable selected = NewOptionsTab.Gameplay
    
    let gameplay_tab = NewOptionsTabContainer(NewOptionsTab.Gameplay)
    let system_tab = NewOptionsTabContainer(NewOptionsTab.System)
    let skins_tab = NewOptionsTabContainer(NewOptionsTab.Noteskins)
    let library_tab = NewOptionsTabContainer(NewOptionsTab.Library)
    
    let change_tab(new_tab: NewOptionsTab) =
        match selected with
        | NewOptionsTab.Gameplay -> gameplay_tab.Hide(true)
        | NewOptionsTab.System -> system_tab.Hide(true)
        | NewOptionsTab.Noteskins -> skins_tab.Hide(true)
        | NewOptionsTab.Library -> library_tab.Hide(true)
        | _ -> ()
        
        match new_tab with
        | NewOptionsTab.Gameplay -> gameplay_tab.Show(true)
        | NewOptionsTab.System -> system_tab.Show(true)
        | NewOptionsTab.Noteskins -> skins_tab.Show(true)
        | NewOptionsTab.Library -> library_tab.Show(true)
        | _ -> ()
        
        selected <- new_tab
    
    do
        keys.Add(NewOptionsItem(NewOptionsTab.Gameplay, fun () -> change_tab(NewOptionsTab.Gameplay)))
        keys.Add(NewOptionsItem(NewOptionsTab.System, fun () -> change_tab(NewOptionsTab.System)))
        keys.Add(NewOptionsItem(NewOptionsTab.Noteskins, fun () -> change_tab(NewOptionsTab.Noteskins)))
        keys.Add(NewOptionsItem(NewOptionsTab.Library, fun () -> change_tab(NewOptionsTab.Library)))
        
    let options_list = NewOptionsList(keys)

    override this.OnBack() =
        (Screen.change ScreenType.MainMenu Transitions.Raw) |> ignore
        None
        
    override this.OnEnter(var0) =
        fade.Target <- 1.0f
        enter_sequence.Add
        <| Animation.seq
            [
                Animation.Action options_list.Show
                Animation.Action (fun () -> gameplay_tab.Show(false))
            ]
        
    override this.OnExit(var0) = ()
    
    override this.Init(parent: Widget) =
        this.With(
            options_list
                .Position(NewOptionsList.HIDDEN_POS),
            gameplay_tab
                .Position(NewOptionsTabContainer.HIDDEN_POS),
            system_tab
                .Position(NewOptionsTabContainer.HIDDEN_POS),
            skins_tab
                .Position(NewOptionsTabContainer.HIDDEN_POS)
        ).Add(
            library_tab
                .Position(NewOptionsTabContainer.HIDDEN_POS)
        )
        base.Init(parent)
    
    override this.Update(elapsed_ms, moved) =
        fade.Update elapsed_ms
        enter_sequence.Update elapsed_ms
        base.Update(elapsed_ms, moved)
    
    override this.Draw() =
        if fade.Value > 0.0f then
            let alpha = 200.0f * fade.Value |> int
            Render.rect this.Bounds (Color.Black.O4a(alpha))
        
            let frame = Content.Texture "settings-frame"
            
            let test_bounds = this.Bounds.ShrinkX(270.0f).ShrinkY(149.0f).TranslateY((Render.height() - 149.0f) * (1.0f - fade.Value))
            
            // Render.sprite test_bounds Color.White frame
            Render.rect test_bounds Color.Black
            
        base.Draw()

