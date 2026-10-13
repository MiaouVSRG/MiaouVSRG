namespace MiaouVSRG.Features.Skins.Browser

open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.Windowing
open Percyqaz.Flux.UI
open Catnip

[<AbstractClass>]
type Thumbnail() as this =
    inherit StaticWidget(NodeType.None)

    let mutable loaded_thumbnail: Sprite option = None
    let fade = Animation.Fade 0.0f
    
    static let mutable instances = []
    
    do
        instances <- this :: instances

    override this.Init(parent) =
        base.Init parent
        this.Load()

    abstract member Load : unit -> unit

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        fade.Update elapsed_ms

    override this.Draw() =

        match loaded_thumbnail with
        | Some p ->
            Render.sprite
                (Sprite.fill this.Bounds p)
                (Colors.white.O4a fade.Alpha) p
        | None -> ()

    member this.FinishLoading(img: Bitmap, label: string) =
        loaded_thumbnail <-
            Some
            <| Sprite.upload_one false LinearSampling (SpriteUpload.OfImage(label, img))
        fade.Target <- 1.0f

    member this.FinishLoading(sprite: Sprite) =
        loaded_thumbnail <- Some sprite
        fade.Target <- 1.0f
        
    member this.Reload() =
        match loaded_thumbnail with
        | Some i ->
            GameThread.defer (fun () -> Sprite.destroy i |> ignore)
        | None -> ()
        this.Load()
        
    static member ReloadAll() =
        Logging.Info $"reloading  {instances.Length} images"
        for instance in instances do instance.Reload()

    override this.Finalize() =
        match loaded_thumbnail with
        | Some i -> GameThread.defer (fun () -> Sprite.destroy i |> ignore)
        | None -> ()
        
        instances <- instances |> List.except [this]