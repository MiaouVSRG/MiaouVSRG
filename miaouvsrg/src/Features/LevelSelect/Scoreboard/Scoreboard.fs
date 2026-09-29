namespace MiaouVSRG.Features.LevelSelect

open Percyqaz.Common
open Percyqaz.Flux.Windowing
open Percyqaz.Flux.UI
open Catnip
open Catnip.Data.User
open MiaouVSRG.Content
open MiaouVSRG.UI
open MiaouVSRG.Options
open MiaouVSRG.Features.Gameplay

[<RequireQualifiedAccess>]
type private Sort =
    | Time = 0
    | Performance = 1
    | Accuracy = 2

[<RequireQualifiedAccess>]
type private Filter =
    | None = 0
    | CurrentRate = 1
    | CurrentMods = 2
    
module ScoreList =
    let SHOW_POS = Position.ShrinkT(AngledButton.HEIGHT).ShrinkB(20.0f).SliceL(460.0f)
    let HIDDEN_POS = Position.ShrinkT(AngledButton.HEIGHT).ShrinkB(20.0f).SliceL(460.0f).TranslateX(-SCREEN_OFFSET)
    
type private ScoreList(scores_list: FlowContainer.Vertical<LocalScoreCard>) =
    inherit SlideContainer(NodeType.None)
    
    let mutable loading = true
    let mutable count = 0
    
    override this.Init(parent: Widget) =
        this.Add(
            ScrollContainer(scores_list),
            EmptyState(Icons.WIND, %"levelselect.info.scoreboard.empty", Subtitle = %"levelselect.info.scoreboard.empty.subtitle")
                .Conditional(fun () -> not loading && count = 0)
        )
        
        base.Init(parent)
        
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        this.Position  <- ScoreList.HIDDEN_POS
        
    member this.Show() : unit =
        this.Position <- ScoreList.SHOW_POS
        
    member this.SetLoading(is_loading: bool) : unit =
        loading <- is_loading
        
    member this.SetCount(value: int) : unit =
        count <- value
    
module ScoreboardHeader =
    let SHOW_POS = Position.SliceT(50.0f)
    let HIDDEN_POS = Position.SliceT(50.0f).TranslateX(-SCREEN_OFFSET)
    
type private ScoreboardHeader(display: Setting<InfoPanelMode>, cycle_sort: unit -> unit, cycle_filter: unit -> unit, sort: Setting<Sort>, filter: Setting<Filter>) =
    inherit SlideContainer(NodeType.None)
    
    override this.Init(parent: Widget) =
        this
            .Add(
                InlaidButton(
                    %"levelselect.info.scoreboard",
                    (fun () -> display.Set InfoPanelMode.Online),
                    ButtonType.CustomSprite "leaderboard-first-button"
                )
                    .Hotkey("scoreboard_storage")
                    .Position(
                        Position
                            .SliceT(50.0f)
                            .GridX(1, 3)
                    )
                    .Help(Help.Info("levelselect.info.mode", "scoreboard_storage")),

                InlaidButton(
                    (fun () ->
                        Icons.CHEVRONS_UP + " " +
                        match sort.Value with
                        | Sort.Accuracy -> %"levelselect.info.scoreboard.sort.accuracy"
                        | Sort.Performance -> %"levelselect.info.scoreboard.sort.performance"
                        | _ -> %"levelselect.info.scoreboard.sort.time"
                    ),
                    cycle_sort,
                    ButtonType.CustomSprite "leaderboard-sort-button"
                )
                    .Hotkey("scoreboard_sort")
                    .Position(
                        Position
                            .SliceT(50.0f)
                            .GridX(2, 3)
                    )
                    .Help(Help.Info("levelselect.info.scoreboard.sort", "scoreboard_sort")),

                InlaidButton(
                    (fun () ->
                        Icons.FILTER + " " +
                        match filter.Value with
                        | Filter.CurrentMods -> %"levelselect.info.scoreboard.filter.currentmods"
                        | Filter.CurrentRate -> %"levelselect.info.scoreboard.filter.currentrate"
                        | _ -> %"levelselect.info.scoreboard.filter.none"
                    ),
                    cycle_filter,
                    ButtonType.CustomSprite "leaderboard-filter-button"
                )
                    .Hotkey("scoreboard_filter")
                    .Position(
                        Position
                            .SliceT(50.0f)
                            .GridX(3, 3)
                    )
                    .Help(Help.Info("levelselect.info.scoreboard.filter", "scoreboard_filter"))
            )
        
        base.Init(parent)
        
    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        
    member this.Hide() : unit =
        this.Position  <- ScoreboardHeader.HIDDEN_POS
        
    member this.Show(with_slide_animation) : unit =
        this.Position <- ScoreboardHeader.SHOW_POS
        if not with_slide_animation then
            this.SnapPosition()

type Scoreboard(display: Setting<InfoPanelMode>) =
    inherit Container(NodeType.None)

    let mutable count = 0
    let mutable loading = true

    let filter = Setting.simple Filter.None
    let sort = Setting.map enum int options.ScoreSortMode

    let sorter () : LocalScoreCard -> LocalScoreCard -> int =
        match sort.Value with
        | Sort.Accuracy -> fun b a -> a.Data.Scoring.Accuracy.CompareTo b.Data.Scoring.Accuracy
        | Sort.Performance -> fun b a -> a.Data.Performance.CompareTo b.Data.Performance
        | Sort.Time
        | _ -> fun b a -> a.Data.TimePlayed.CompareTo b.Data.TimePlayed

    let filterer () : LocalScoreCard -> bool =
        match filter.Value with
        | Filter.CurrentRate -> (fun a -> a.Data.Rate = SelectedChart.rate.Value)
        | Filter.CurrentMods -> (fun a -> a.Data.Mods = SelectedChart.selected_mods.Value)
        | _ -> K true

    let scores_list =
        FlowContainer.Vertical<LocalScoreCard>(75.0f, Spacing = Style.PADDING * 3.0f)
        
    let score_list_container = ScoreList(scores_list)

    do
        LocalScores.score_loaded.Add (fun score_info -> score_info |> LocalScoreCard |> scores_list.Add; count <- count + 1; score_list_container.SetCount(count + 1))
        LocalScores.scores_loaded.Add (fun () ->
            loading <- false
            score_list_container.SetLoading(false)
        )

    let refresh_filter () =
        scores_list.Filter <- filterer ()

    let cycle_filter () =
        filter.Value <-
            match filter.Value with
            | Filter.CurrentMods -> Filter.None
            | Filter.CurrentRate -> Filter.CurrentMods
            | _ -> Filter.CurrentRate
        refresh_filter()

    let cycle_sort () =
        sort.Value <-
            match sort.Value with
            | Sort.Performance -> Sort.Accuracy
            | Sort.Accuracy -> Sort.Time
            | _ -> Sort.Performance
        scores_list.Sort <- sorter ()
        
    let scoreboard_header = ScoreboardHeader(display, cycle_sort, cycle_filter, sort, filter)

    override this.Init(parent: Widget) =
        SelectedChart.on_chart_change_started.Add (fun _ -> scores_list.Iter(fun s -> s.FadeOut()); loading <- true; score_list_container.SetLoading(true))
        SelectedChart.on_chart_change_finished.Add (fun _ -> scores_list.Clear(); count <- 0; score_list_container.SetCount(0))
        Rulesets.on_changed.Add (fun _ -> GameThread.defer (fun () -> scores_list.Sort <- sorter ()))
        SelectedChart.on_chart_update_finished.Add (fun _ -> refresh_filter())
        Gameplay.score_deleted.Add (fun timestamp -> scores_list.Iter(fun sc -> if sc.Data.TimePlayed = timestamp then GameThread.defer (fun () -> scores_list.Remove sc |> ignore)))
        scores_list.Sort <- sorter ()

        this
            .Add(
                scoreboard_header
                    .Position(ScoreboardHeader.HIDDEN_POS),

                score_list_container
                    .Position(ScoreList.HIDDEN_POS),

                HotkeyListener(
                    "scoreboard",
                    fun () ->
                        if scores_list.Focused then
                            Selection.clear ()
                        else
                            scores_list.Focus false
                )
            )
        base.Init(parent)

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        LocalScores.score_loader.Join()
        
    member this.Show(with_slide_animation: bool) =
        scoreboard_header.Show(with_slide_animation)
        score_list_container.Show()
        
    member this.Hide() =
        scoreboard_header.Hide()
        score_list_container.Hide()