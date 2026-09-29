namespace MiaouVSRG.UI

open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Catnip

type PageSetting(localised_text: string, widget: Widget) as this =
    inherit Container(NodeType.Container(fun _ -> Some this.Child))

    let mutable widget = widget

    member this.Child
        with get () = widget
        and set (w: Widget) =
            let old_widget = widget
            widget <- w
            w.Position <- Position.ShrinkL(PAGE_LABEL_WIDTH).ShrinkR(Style.PADDING * 20.0f).Shrink(Style.PADDING)

            if this.Initialised then
                w.Init this

                if old_widget.Focused then
                    w.Focus false

    override this.Init(parent: Widget) =
        this
            .Add(
                Text(localised_text)
                    .Color((Colors.white, Colors.TRANSPARENT))
                    .Align(Alignment.LEFT)
                    .Position(Position.SliceT(PAGE_ITEM_HEIGHT).SliceL(PAGE_LABEL_WIDTH).ShrinkY(Style.PADDING * 2.0f).ShrinkL(15.0f))
            )

        base.Init parent
        widget
            .Position(Position.ShrinkL(PAGE_LABEL_WIDTH).ShrinkR(Style.PADDING * 20.0f).Shrink(Style.PADDING))
            .Init(this)

    override this.Draw() =
        // if widget.Selected then
        //     Render.rect (widget.Bounds.Expand(15.0f, Style.PADDING)) Colors.pink_accent.O2
        Render.rect this.Bounds Colors.dark_gray
        base.Draw()
        widget.Draw()

    override this.Update(elapsed_ms, moved) =
        base.Update(elapsed_ms, moved)
        widget.Update(elapsed_ms, moved)