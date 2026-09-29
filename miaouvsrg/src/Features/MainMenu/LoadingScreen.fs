namespace MiaouVSRG.Features.MainMenu

open Percyqaz.Common
open Percyqaz.Flux.Audio
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Windowing
open Percyqaz.Flux.UI
open MiaouVSRG.Content
open MiaouVSRG.Options
open MiaouVSRG.UI
open MiaouVSRG.Features.Online
open Catnip
open Catnip.Data.Library

type LoadingScreen(post_init_thunk: unit -> unit) =
    inherit Screen()

    let mutable closing = false
    let audio_fade = Animation.Fade 0.0f
    let animation = Animation.Sequence()
    let background_fade = Animation.Delay(1500.0)
    let finished_loading_animation = Animation.Delay(800.0)
    let loading_container = LoadingIndicator.Percentage(0)
    let mutable finished_loading = false

    let post_init () =
        async {
            try
                post_init_thunk ()
                Ok()
            with error ->
                Error error
            |>
            function
            | Ok () ->
                finished_loading <- true
                GameThread.defer
                <| fun () ->
                animation.Add(Animation.Delay 50.0)
                animation.Add(Animation.Action(fun () -> audio_fade.Target <- 1.0f))
                animation.Add(Animation.Action(fun () -> Screen.logo.MoveMenu()))
                animation.Add(Animation.Action(fun () -> Screen.change ScreenType.MainMenu Transitions.Raw |> ignore))
            | Error error ->
                GameThread.defer (fun () -> raise error)
        }
        |> Async.Start

    override this.Init(parent: Widget) =
        this
        |* loading_container
            .Position(Position.SliceB(150.0f, 100.0f).SliceX(500.0f))
        base.Init parent

    override this.OnEnter(prev: ScreenType) =
        Toolbar.hide ()

        background_fade.Reset()
        finished_loading_animation.Reset()

        match prev with
        | ScreenType.SplashScreen ->
            animation.Add(Animation.Action(fun () -> Sounds.get("hello").Play()))
            animation.Add(Animation.Delay 100.0)
            animation.Add(Animation.Action(fun () -> Screen.logo.MoveTopLeft(false)))
            animation.Add(Animation.Delay 900.0)
            animation.Add(Animation.Action(post_init))
        | _ ->
            Screen.logo.MoveTopLeft(true)
            closing <- true
            DiscordRPC.clear()
            audio_fade.Snap()
            animation.Add(Animation.Action(fun () -> audio_fade.Target <- 0.0f))
            animation.Add(Animation.Delay 1000.0)
            animation.Add(Animation.Delay 600.0)
            animation.Add(Animation.Action(fun () -> Sounds.get("goodbye").Play()))
            animation.Add(Animation.Delay 500.0)
            animation.Add(Animation.Action(fun () -> Screen.back Transitions.Default |> ignore))

    override this.OnExit _ =
        if not closing then
            Audio.change_volume (options.AudioVolume.Value, options.AudioVolume.Value)

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        audio_fade.Update elapsed_ms
        animation.Update elapsed_ms
        background_fade.Update elapsed_ms

        Audio.change_volume (
            options.AudioVolume.Value,
            options.AudioVolume.Value * float audio_fade.Value
        )
        
        if finished_loading then
            finished_loading_animation.Update elapsed_ms

    override this.Draw() =
        let loaded_charts = ChartDatabase.LOADED_CHARTS
        
        
        // TODO: Optimize this count (set chart count in options.json stored while closing the game)
        // so it won't load every time the user opens the game
        let total_charts = ChartDatabase.CHART_COUNT
        loading_container.Count <- loaded_charts
        loading_container.TotalCount <- total_charts
        let alpha =
            if closing then
                255.0 * (1.0 - background_fade.Progress) |> int
            else
                255.0 * background_fade.Progress |> int
        
        
        Render.sprite this.Bounds (Color.White.O4a alpha) (Content.Texture "loading-screen")

        if closing then
            Text.draw_aligned_b (
                Style.font,
                "goodbye ^^",
                70.0f,
                this.Bounds.CenterX,
                this.Bounds.CenterY,
                Colors.text,
                0.5f
            )
        else
            if finished_loading then
                let text_alpha = 255.0 * ( 1.0 - finished_loading_animation.Progress) |> int
                loading_container.Alpha <- text_alpha
                        
                Text.draw_aligned_b (
                    Style.font,
                    "loading :3",
                    50.0f,
                    this.Bounds.CenterX,
                    775.0f,
                    (Colors.white.O4a text_alpha, Colors.black.O4a text_alpha),
                    0.5f
                )
                if total_charts > 0 then
                    Text.draw_aligned_b (
                        Style.font,
                        // Not only the charts are loaded but it takes 95% of the loading time
                        $"{loaded_charts} / {total_charts} charts loaded",
                        20.0f,
                        this.Bounds.CenterX,
                        920.0f,
                        (Colors.white.O4a text_alpha, Colors.black.O4a text_alpha),
                        0.5f
                    )
            else
                Text.draw_aligned_b (
                    Style.font,
                    "loading :3",
                    50.0f,
                    this.Bounds.CenterX,
                    775.0f,
                    (Colors.white, Colors.black),
                    0.5f
                )
                if total_charts > 0 then
                    Text.draw_aligned_b (
                        Style.font,
                        // Not only the charts are loaded but it takes 95% of the loading time
                        $"{loaded_charts} / {total_charts} charts loaded",
                        20.0f,
                        this.Bounds.CenterX,
                        920.0f,
                        (Colors.white, Colors.black),
                        0.5f
                    )
            base.Draw()

    override this.OnBack() =
        WindowThread.exit()
        None