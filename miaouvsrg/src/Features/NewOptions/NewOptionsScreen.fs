namespace MiaouVSRG.Features.OptionsScreen

open System.Linq
open MiaouVSRG.Content
open MiaouVSRG.Features.OptionsMenu.Gameplay
open MiaouVSRG.Features.OptionsMenu.SystemSettings
open MiaouVSRG.Features.Pacemaker
open MiaouVSRG.Features.Skins
open MiaouVSRG.Options
open MiaouVSRG.UI
open Percyqaz.Flux.Windowing
open Catnip
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
    
module SettingsWidgets =
    
    type Utils =
        static member Subcategory(name: string) : Text =
            Text(name).Color((Colors.purple_accent, Colors.TRANSPARENT)).Align(Alignment.LEFT)
    
    type Gameplay =
    
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
                        .Color((Colors.purple_accent, Colors.TRANSPARENT))
                        .Align(Alignment.LEFT)
                        .Position(page_position(1, 1, PageWidth.Normal).ShrinkL(PAGE_LABEL_WIDTH).ShrinkY(3.0f).TranslateY(5.0f))
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
            
    module Gameplay =
        type LaneCover =
            static member LanecoverEnabled() : PageSetting =
                PageSetting(%"gameplay.lanecover.enabled", Checkbox options.LaneCover.Enabled)
                
            static member DrawUnderReceptors() : PageSetting =
                PageSetting(%"gameplay.lanecover.draw_under_receptors", Checkbox options.LaneCover.DrawUnderReceptors)
                
            static member Hidden() : PageSetting =
                PageSetting(%"gameplay.lanecover.hidden", Slider.Percent(options.LaneCover.Hidden))
                    .Help(Help.Info("gameplay.lanecover.hidden"))
                
            static member Sudden() : PageSetting =
                PageSetting(%"gameplay.lanecover.sudden", Slider.Percent(options.LaneCover.Sudden))
                    .Help(Help.Info("gameplay.lanecover.sudden"))
            
            static member FadeLength() : PageSetting =
                PageSetting(%"gameplay.lanecover.fadelength", Slider(options.LaneCover.FadeLength, Step = 5.0f))
                    .Help(Help.Info("gameplay.lanecover.fadelength"))
            
            static member Color() : PageSetting =
                PageSetting(%"gameplay.lanecover.color", ColorPicker(%"gameplay.lanecover.color", options.LaneCover.Color, true))

            static member Pacemaker() : PageButton =
                PageButton(%"gameplay.pacemaker", fun () -> PacemakerOptionsPage().Show())
                    .Help(Help.Info("gameplay.pacemaker"))
            
    type System = 
    
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
            FlowContainer.Vertical<Widget>(60.0f, Spacing = 10.0f)
                .With(
                    SettingsWidgets.Utils.Subcategory("General"),
                    SettingsWidgets.Gameplay.ScrollSpeed(),
                    SettingsWidgets.Gameplay.HitPosition(),
                    SettingsWidgets.Gameplay.Upscroll(),
                    SettingsWidgets.Gameplay.BackgroundDim(),
                    SettingsWidgets.Gameplay.HoldToGiveUp(),
                    SettingsWidgets.Gameplay.HideHitNotes(),
                    SettingsWidgets.Gameplay.OnQuitOut(),
                    SettingsWidgets.Utils.Subcategory("Lanecover"),
                    SettingsWidgets.Gameplay.LaneCover.LanecoverEnabled(),
                    SettingsWidgets.Gameplay.LaneCover.DrawUnderReceptors(),
                    SettingsWidgets.Gameplay.LaneCover.Hidden(),
                    SettingsWidgets.Gameplay.LaneCover.Sudden(),
                    SettingsWidgets.Gameplay.LaneCover.FadeLength(),
                    SettingsWidgets.Gameplay.LaneCover.Color(),
                    SettingsWidgets.Utils.Subcategory("Pacemaker"),
                    SettingsWidgets.Gameplay.LaneCover.Pacemaker(),
                    SettingsWidgets.Utils.Subcategory("Keybinds")
                )
        
        for keys = 3 to 10 do
            nav_gameplay.Add(PageSetting($"{keys}K", GameplayKeybinder (enum keys)))
                
        let nav_system =
            NavigationContainer.Column()
                .WrapNavigation(false)
                .With(
                    SystemPage.WindowMode().Pos(0),
                    SettingsWidgets.System.WindowedResolution().Pos(2),
                    SettingsWidgets.System.Monitor().Pos(2),
                    SettingsWidgets.System.VideoMode().Pos(4),
                    SettingsWidgets.System.LetterboxResolution().Pos(4),

                    SettingsWidgets.System.Performance().Pos(7),
                    SettingsWidgets.System.Hotkeys().Pos(9),

                    PageButton(%"system.audio", fun () -> AudioPage().Show()).Pos(12),
                    SettingsWidgets.System.VisualOffset().Pos(14)
                )
                
        let container_test = ScrollContainer(nav_gameplay)
        
        this
            .WithConditional((tab = NewOptionsTab.Gameplay), container_test)
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

