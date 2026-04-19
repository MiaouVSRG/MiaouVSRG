namespace Interlude.Features.LevelSelect

open Percyqaz.Common
open Percyqaz.Flux.Input
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Audio
open Percyqaz.Flux.UI
open Prelude
open Prelude.Data.Library
open Interlude.Content
open Interlude.Options
open Interlude.Features.Gameplay
open Interlude.UI
open Interlude.Features.Online
open Interlude.Features.Play

type LevelSelectScreen() =
    inherit Screen()

    let TOP_BAR_HEIGHT = 150.0f
    let INFO_SCREEN_SPLIT = 0.4f
    let BULK_ACTION_BUTTON_WIDTH = 300.0f
    
    // 2 * full InlaidButton width (sort and group by) +
    // 3 * semi InlaidButton width (randomize chart, context menu and levelselect options buttons) +
    // 4 * 5px gaps between them
    let LIBRARY_VIEW_WIDTH = 3.5f * InlaidButton.WIDTH + 20.0f

    let search_text = Setting.simple ""
    
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
                CurrentChart()
                    .Position(Position.SliceT(TOP_BAR_HEIGHT).SlicePercentL(INFO_SCREEN_SPLIT)),

                SearchBox(search_text, fun f ->
                    LevelSelect.filter <- f
                    Tree.refresh ()
                )
                    .Position(
                        Position
                            .SliceT(TOP_BAR_HEIGHT / 1.5f)
                            .ShrinkB(AngledButton.HEIGHT)
                            .SliceY(SearchBox.HEIGHT)
                            .ShrinkPercentL(0.4f)
                            .ShrinkL(500.0f)
                            .ShrinkR((TOP_BAR_HEIGHT - AngledButton.HEIGHT - SearchBox.HEIGHT - Style.PADDING) * 0.5f)
                    )
                    .Help(Help.Info("levelselect.search", "search")),

                InfoPanel()
                    .Position(Position.ShrinkT(TOP_BAR_HEIGHT + 5.0f).SlicePercentL(INFO_SCREEN_SPLIT)),

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
            .WithConditional(
                (fun () -> Tree.multi_selection().IsNone),

                InlaidButton(
                    sprintf "%s %s" Icons.PLAY %"levelselect.play",
                    LevelSelect.choose_this_chart,
                    ButtonType.Default
                )
                    .Position(Position.SliceB(InlaidButton.HEIGHT).SliceR(InlaidButton.WIDTH * 1.2f))
                    .Help(Help.Info("levelselect.play", "select"))
            )
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
                LibraryViewControls()
                    .Position(Position.SliceT(TOP_BAR_HEIGHT / 1.35f).SliceB(50.0f).ShrinkPercentL(0.55f).SliceR(LIBRARY_VIEW_WIDTH))
            )

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)

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

    override this.Draw() =

        Tree.draw (this.Bounds.Top + TOP_BAR_HEIGHT / 1.35f, this.Bounds.Bottom)

        base.Draw()

    override this.OnEnter(_: ScreenType) =
        LevelSelect.exit_gameplay()
        Song.on_finish <- SongFinishAction.LoopFromPreview
        
        Toolbar.show(true, false)

        Tree.refresh ()
        DiscordRPC.in_menus ("Choosing a song")

    override this.OnExit(_: ScreenType) = Input.remove_listener ()

    override this.OnBack() =
        if Network.lobby.IsSome then
            Some ScreenType.Lobby
        else
            Some ScreenType.MainMenu