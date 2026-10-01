namespace MiaouVSRG.Features.LevelSelect

open Percyqaz.Common
open Percyqaz.Flux.Input
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Audio
open Percyqaz.Flux.UI
open Catnip
open Catnip.Data.Library
open MiaouVSRG.Content
open MiaouVSRG.Options
open MiaouVSRG.Features.Gameplay
open MiaouVSRG.UI
open MiaouVSRG.Features.Online
open MiaouVSRG.Features.Play

type LevelSelectScreen() =
    inherit Screen()
    let BULK_ACTION_BUTTON_WIDTH = 300.0f

    let search_text = Setting.simple ""
    
    let current_chart_container = CurrentChart()
    let info_panel_container = InfoPanel()
    let library_view_controls_container = LibraryViewControls()
    let searchbox_container = SearchBox(search_text, fun f -> LevelSelect.filter <- f; Tree.refresh ())
    
    let enter_screen_sequence = Animation.Group()
    let exit_screen_sequence = Animation.Group()
    let mutable enter = false
    let mutable exit = false
    
    let slide_animation = Animation.Fade 0.0f
    
    member this.ApplyKeymodeFilter(keymode : int) =
        let inner_filter = LevelSelect.filter.Filter
        LevelSelect.filter <-
            { LevelSelect.filter with
                Filter =
                    { inner_filter with
                        Keymode = Some keymode
                    }
            }
        Tree.refresh ()
        LevelSelect.refresh_all ()

    override this.Init(parent: Widget) =
        base.Init parent

        LevelSelect.on_refresh_all.Add Tree.refresh
        Rulesets.on_changed.Add (fun _ ->
            match options.ChartGroupMode.Value with
            | "grade"
            | "lamp" -> LevelSelect.refresh_all()
            | _ -> LevelSelect.refresh_details()
        )

        if not (Sorting.modes.ContainsKey options.ChartSortMode.Value) then
            options.ChartSortMode.Value <- "title"
        if not (Grouping.modes.ContainsKey options.ChartGroupMode.Value) then
            options.ChartGroupMode.Value <- "pack"

        this
            .With(
                current_chart_container
                    .Position(CurrentChart.HIDDEN_POS),
                searchbox_container
                    .Position(SearchBoxPositionsForLevelSelect.HIDDEN_POS)
                    .Help(Help.Info("levelselect.search", "search")),

                info_panel_container
                    .Position(InfoPanel.SHOW_POS),

                // Empty states explaining why there are no charts to show
                Container(NodeType.None)
                    .Position(Position.ShrinkT(TOP_BAR_HEIGHT).ShrinkPercentL(INFO_SCREEN_SPLIT))
                    .WithConditional(
                        (fun () -> search_text.Value <> ""),
                        EmptyState(Icons.SEARCH, %"levelselect.empty.search")
                    )
                    .WithConditional(
                        (fun () ->
                            search_text.Value = ""
                            && options.ChartGroupMode.Value = "level"
                            && Content.Table.IsNone
                        ),
                        EmptyState(Icons.SIDEBAR, %"levelselect.empty.no_table")
                    )
                    .WithConditional(
                        (fun () ->
                            search_text.Value = ""
                            && options.ChartGroupMode.Value = "collection"
                        ),
                        EmptyState(Icons.FOLDER, %"levelselect.empty.no_collections")
                    )
                    .WithConditional(
                        (fun () ->
                            search_text.Value = ""
                            && options.ChartGroupMode.Value <> "collection"
                            && options.ChartGroupMode.Value <> "level"
                        ),
                        EmptyState(Icons.FOLDER, %"levelselect.empty.no_charts")
                    )
                    .Conditional(fun () -> Tree.is_empty)
            )
            // Normal chart actions (no bulk select)
            // .WithConditional(
            //     (fun () -> Tree.multi_selection().IsNone),
            //
            //     InlaidButton(
            //         sprintf "%s %s" Icons.PLAY %"levelselect.play",
            //         LevelSelect.choose_this_chart,
            //         ButtonType.Default
            //     )
            //         .Position(Position.SliceB(InlaidButton.HEIGHT).SliceR(InlaidButton.WIDTH * 1.2f))
            //         .Help(Help.Info("levelselect.play", "select"))
            // )
            
            // Bulk select actions
            .WithConditional(
                (fun () -> Tree.multi_selection().IsSome),

                AngledButton(
                    sprintf "%s %s" Icons.X %"levelselect.clear_multi_selection",
                    (fun () -> Tree.clear_multi_selection(); Tree.debounce()),
                    Palette.DARK.O2
                )
                    .LeanRight(false)
                    .Position(Position.SliceB(AngledButton.HEIGHT).SliceR(BULK_ACTION_BUTTON_WIDTH)),

                AngledButton(
                    sprintf "%s %s" Icons.LIST %"bulk_actions",
                    (fun () -> match Tree.multi_selection() with Some s -> s.ShowActions() | None -> ()),
                    Palette.MAIN.O2
                )
                    .Position(Position.SliceB(AngledButton.HEIGHT).SliceR(BULK_ACTION_BUTTON_WIDTH).TranslateX(-BULK_ACTION_BUTTON_WIDTH - AngledButton.LEAN_AMOUNT))
            )
            .Add(
                // Goes last so that its dropdowns draw over action buttons
                library_view_controls_container
                    .Position(LibraryViewControls.HIDDEN_POS)
            )

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
            
        slide_animation.Update elapsed_ms

        if (%%"select").Pressed() then
            LevelSelect.choose_this_chart ()

        elif (%%"next").Pressed() then
            Tree.next ()
        elif (%%"previous").Pressed() then
            Tree.previous ()
        elif (%%"next_group").Pressed() then
            Tree.next_group ()
        elif (%%"previous_group").Pressed() then
            Tree.previous_group ()
        elif (%%"start").Pressed() then
            Tree.top_of_group ()
        elif (%%"end").Pressed() then
            Tree.bottom_of_group ()

        Tree.update (this.Bounds.Top + TOP_BAR_HEIGHT / 1.35f, this.Bounds.Bottom, elapsed_ms)
        
        if not exit && enter then
            enter_screen_sequence.Update elapsed_ms
            
        if not enter && exit then
            exit_screen_sequence.Update elapsed_ms

    override this.Draw() =

        Tree.draw (this.Bounds.Top + TOP_BAR_HEIGHT / 1.35f, this.Bounds.Bottom, slide_animation.Value)

        base.Draw()

    override this.OnEnter(_: ScreenType) =
        LevelSelect.exit_gameplay()
        Song.on_finish <- SongFinishAction.LoopFromPreview
        
        Toolbar.show(true, false)

        Tree.refresh ()
        
        enter_screen_sequence.Add
        <| Animation.seq
            [
                Animation.Action current_chart_container.Show
                Animation.Action info_panel_container.Show
                Animation.Action library_view_controls_container.Show
                Animation.Action searchbox_container.Show
                Animation.Delay 650.0
                Animation.Action (fun () -> slide_animation.Target <- 1.0f)
                Animation.Action (fun () -> Screen.reset_default_background_fade())
            ]
            
        exit_screen_sequence.Add
        <| Animation.seq
            [
                Animation.Action(fun () -> Screen.start_default_background_fade(true))
                Animation.Action current_chart_container.Hide
                Animation.Action info_panel_container.Hide
                Animation.Delay 100.0
                Animation.Action (fun () -> slide_animation.Target <- 0.0f)
                Animation.Action library_view_controls_container.Hide
                Animation.Action searchbox_container.Hide
                Animation.Delay 300.0
                Animation.Action (fun () -> slide_animation.Snap())
                Animation.Action(fun () -> Screen.change ScreenType.MainMenu Transitions.Raw |> ignore)
            ]
            
        enter <- true
        exit <- false
        
        DiscordRPC.in_menus ("Choosing a song")

    override this.OnExit(_: ScreenType) = Input.remove_listener ()

    override this.OnBack() =
        if Network.lobby.IsSome then
            Some ScreenType.Lobby
        else
            exit <- true
            enter <- false
            None