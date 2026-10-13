namespace MiaouVSRG.Features.LevelSelect

open System.Drawing
open Catnip
open Catnip.Data
open MiaouVSRG.Content
open MiaouVSRG.Features.LevelSelect
open MiaouVSRG.Features.Online
open MiaouVSRG.Features.Skins.Browser
open MiaouVSRG.UI
open MiaouVSRG.Web.Shared.Requests
open Percyqaz.Common
open Percyqaz.Flux.Graphics
open Percyqaz.Flux.UI
open Percyqaz.Flux.Windowing


type private UserInfos() =
    inherit Container(NodeType.None)
    
    // Height of one element (miniprofile sprite has a height of 125px)
    // We divide by 3 because there are three sections :
    // - The username
    // - The stats (acc and rating, smaller than the other)
    // - The rank (bigger than the other)
    let BASE_HEIGHT = 125.0f / 3.0f
    
    let username = Network.credentials.Username
    let mutable acc = 0.0
    let mutable rating = 0.0
    let mutable global_ranking = 0
    
    member this.LoadStats(stats: Players.Miniprofile.Response) =
        acc <- stats.MeanAccuracy * 100.0
        rating <- stats.OverallRating
        global_ranking <- stats.GlobalRank
    
    override this.Init(parent: Widget) =
        this
            .With(
                Text(username)
                    .Position(Position.SliceT(BASE_HEIGHT))
                    .Align(Alignment.LEFT)
                    .Color((Color.White, Color.Transparent)),
                Container(NodeType.None)
                    .With(
                        Text(fun () ->
                            if Network.status.IsLoggedIn then
                                sprintf "Accuracy: %.2f%%" acc
                            else
                                "Not connected"
                        )
                            .Position(Position.GridY(1, 3))
                            .Align(Alignment.LEFT)
                            .Color((Color.White, Color.Transparent)),
                        Text(fun () ->
                            if Network.status.IsLoggedIn then
                                sprintf "Rating: %.2f" rating
                            else
                                "Click to login !"
                        )
                            .Position(Position.GridY(2, 3))
                            .Align(Alignment.LEFT)
                            .Color((Color.White, Color.Transparent))
                    )
                    .Position(Position.ShrinkT(0.8f * BASE_HEIGHT).SliceT(1.6f * BASE_HEIGHT))
            )
            .Add(
                Text(fun () ->
                    if Network.status.IsLoggedIn then
                        sprintf "#%i" global_ranking
                    else
                        ""
                )
                    .Position(Position.SliceB(1.5f * BASE_HEIGHT))
                    .Color(((Color.White.O4a 150), Color.Transparent))
                    .Align(Alignment.RIGHT)
            )
        base.Init(parent)
        
module ProfileBox =
    let HIDDEN_POS = Position.SliceB(105.0f).ShrinkPercentL(INFO_SCREEN_SPLIT + 0.01f).ShrinkR(CHART_WIDTH - CHART_LEFT_MARGIN + 20.0f).TranslateY(SCREEN_OFFSET)
    let HIDDEN_POS_DOS = Position.SliceB(105.0f).ShrinkPercentL(INFO_SCREEN_SPLIT + 0.01f).ShrinkR(CHART_WIDTH - CHART_LEFT_MARGIN + 20.0f).TranslateY(500.0f)
    let SHOW_POS = Position.SliceB(105.0f).ShrinkPercentL(INFO_SCREEN_SPLIT + 0.01f).ShrinkR(CHART_WIDTH - CHART_LEFT_MARGIN + 20.0f)

type ProfileBox() =
    inherit SlideContainer(NodeType.None)
    
    // By default, the "miniprofile" sprite has a border of 7px and a margin of 6px
    // which is approximately 15px, i.e. Style.PADDING * 3
    let PADDING_MINIPROFILE = Style.PADDING * 3.0f
    
    let MARGIN = Style.PADDING * 2.0f
    
    let user_infos_box = UserInfos()
    
    let mutable download_link = "https://cdn.miaouvsrg.com/avatars/empty.png"
    
    let thumbnail =
        { new Thumbnail() with
            override this.Load() =
                ImageServices.get_cached_image.Request(
                    download_link,
                    function
                    | Some img -> GameThread.defer (fun () -> this.FinishLoading(img, "AVATAR_PREVIEW"))
                    | None -> Logging.Warn "Failed to load avatar thumbnail from '%s'" download_link
                )
        }
        
    member this.Load(infos: Players.Miniprofile.Response) =
        user_infos_box.LoadStats(infos)
        download_link <- infos.ProfilePictureLink
        thumbnail.Load()
    
    override this.Init(parent: Widget) =
        let POSITION_RIGHT_TO_PFP = Position.ShrinkX(PADDING_MINIPROFILE).ShrinkL(90.0f + MARGIN).ShrinkT(PADDING_MINIPROFILE)
        this
            .With(
                thumbnail
                    .Position(Position.Shrink(PADDING_MINIPROFILE).SliceL(90.0f).SliceT(90.0f)),
                user_infos_box
                    .Position(POSITION_RIGHT_TO_PFP)
            )
            .Add(
                MouseListener()
                    .OnLeftClick(fun() ->
                        if Network.status.IsLoggedIn then
                            open_url($"https://www.beta.miaouvsrg.com/user/profile/{Network.credentials.Username}")
                        elif Network.status.IsConnected then
                            Menu.ShowPage LoginPage
                    )
            )
        base.Init(parent)
    
    override this.Draw() =
        let sprite = Content.Texture "miniprofile"
        Render.sprite this.Bounds Color.White sprite
        base.Draw()
        
    member this.Show() =
        this.Position <- ProfileBox.SHOW_POS
        
    member this.Hide() =
        this.Position <- ProfileBox.HIDDEN_POS_DOS
        