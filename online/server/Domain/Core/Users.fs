namespace Interlude.Web.Server.Domain.Core

open System
open System.Security.Cryptography
open Microsoft.FSharp.Collections
open Percyqaz.Common
open Prelude
open Percyqaz.Data.Sqlite
open Interlude.Web.Server
open BCrypt.Net

type Badge = string

module Badge =

    let DEVELOPER = "developer"
    let DONATOR = "donator"
    let MODERATOR = "moderator"
    let EARLYTESTER = "early-tester"
    let TABLE_EDITOR = "table-editor"
    let CONTRIBUTOR = "contributor"

    let badge_color (badge: Badge) : int32 list =
        match badge with
        | _ when badge = EARLYTESTER -> [ 0xFF_66ff6e ]
        | _ when badge = MODERATOR -> [ 0xFF_66c2ff ]
        | _ when badge = DEVELOPER -> [ 0xFF_ff7559 ]
        | _ when badge = DONATOR -> [ 0xFF_ff8cdd; 0xFF_ffd36e ]
        | _ when badge = CONTRIBUTOR -> [ 0xFF_873dff ]
        | _ -> []

    let DEFAULT_COLOR = 0xFF_cecfd9

type User =
    {
        Username: string
        DiscordId: uint64 // TODO: make it optional for website login
        DateSignedUp: int64
        LastLogin: int64
        AuthToken: string
        Badges: Set<Badge>
        Color: int32
        Password: string option
        ProfileBanner: string
        ProfilePicture: string
        CountryFlag: string option
        Coins: int64
        
        // Hex code for website custom theme
        PrimaryColor: string option
        SecondaryColor: string option
        TextColor: string option
        
        // About me on website
        AboutMe: string option
        
        // Background image on website
        BackgroundImage: string option
    }
    
    member this.WithCustomTheme(primary: string, secondary: string) =
        {
            this with
                PrimaryColor = Some primary
                SecondaryColor = Some secondary
        }
        
    member this.WithTextColor(color: string) =
        {
            this with
                TextColor = Some color
        }
        
    member this.WithAboutMe(content: string) =
        {
            this with
                AboutMe = Some content
        }
        
    member this.WithBackgroundImage(background_url: string) =
        {
            this with
                BackgroundImage = Some background_url
        }

module User =

    let internal TABLE: TableCommandHelper =
        {
            Name = "users"
            PrimaryKey = Column.Integer("Id").Unique
            Columns =
                [
                    Column.Text("Username").Unique
                    Column.Text("DiscordId")
                    Column.Integer("DateSignedUp")
                    Column.Integer("LastLogin")
                    Column.Text("AuthToken")
                    Column.Text("Badges")
                    Column.Integer("Color")
                    Column.Text("Password").Nullable
                    Column.Text("ProfileBanner")
                    Column.Text("ProfilePicture")
                    Column.Text("CountryFlag").Nullable
                    Column.Integer("Coins")
                    Column.Text("PrimaryColor").Nullable
                    Column.Text("SecondaryColor").Nullable
                    Column.Text("AboutMe").Nullable
                    Column.Text("WebBackgroundImage").Nullable
                    Column.Text("TextColor").Nullable
                ]
        }

    let generate_auth_token () =
        RandomNumberGenerator.GetBytes(27)
        |> Convert.ToBase64String

    let create_with_discord (username, discord_id) =
        {
            Username = username
            DiscordId = discord_id
            DateSignedUp = Timestamp.now ()
            LastLogin = 0L
            AuthToken = generate_auth_token ()
            Badges = Set.empty
            Color = Badge.DEFAULT_COLOR
            Password = None
            ProfileBanner = "https://cdn.miaouvsrg.com/banners/empty.png"
            ProfilePicture = "https://cdn.miaouvsrg.com/avatars/empty.png"
            CountryFlag = None
            Coins = 0
            PrimaryColor = None
            SecondaryColor = None
            AboutMe = None
            BackgroundImage = None
            TextColor = None
        }
        
    let create_with_password (username: string, password: string) =
        let salt = BCrypt.GenerateSalt()
        {
            Username = username
            DiscordId = uint64 0
            DateSignedUp = Timestamp.now()
            LastLogin = 0L
            AuthToken = generate_auth_token()
            Badges = Set.empty
            Color = Badge.DEFAULT_COLOR
            Password = Some (BCrypt.HashPassword(password, salt))
            ProfileBanner = "https://cdn.miaouvsrg.com/banners/empty.png"
            ProfilePicture = "https://cdn.miaouvsrg.com/avatars/empty.png"
            CountryFlag = None
            Coins = 0
            PrimaryColor = None
            SecondaryColor = None
            AboutMe = None
            BackgroundImage = None
            TextColor = None
        }

    let private SAVE_NEW: NonQuery<User> =
        {
            SQL = TABLE.INSERT
            Parameters =
                [
                    "@Username", SqliteType.Text, -1
                    "@DiscordId", SqliteType.Text, -1
                    "@DateSignedUp", SqliteType.Integer, 8
                    "@LastLogin", SqliteType.Integer, 8
                    "@AuthToken", SqliteType.Text, -1
                    "@Badges", SqliteType.Text, -1
                    "@Color", SqliteType.Integer, 8
                    "@Password", SqliteType.Text, -1
                    "@ProfileBanner", SqliteType.Text, -1
                    "@ProfilePicture", SqliteType.Text, -1
                    "@CountryFlag", SqliteType.Text, -1
                    "@Coins", SqliteType.Integer, 8
                    "@PrimaryColor", SqliteType.Text, -1
                    "@SecondaryColor", SqliteType.Text, -1
                    "@AboutMe", SqliteType.Text, -1
                    "@WebBackgroundImage", SqliteType.Text, -1
                    "@TextColor", SqliteType.Text, -1
                ]
            FillParameters =
                (fun p user ->
                    p.String user.Username
                    p.String(string user.DiscordId)
                    p.Int64 user.DateSignedUp
                    p.Int64 user.LastLogin
                    p.String user.AuthToken
                    p.Json JSON user.Badges
                    p.Int32 user.Color
                    p.StringOption user.Password
                    p.String user.ProfileBanner
                    p.String user.ProfilePicture
                    p.StringOption user.CountryFlag
                    p.Int64 user.Coins
                    p.StringOption user.PrimaryColor
                    p.StringOption user.SecondaryColor
                    p.StringOption user.AboutMe
                    p.StringOption user.BackgroundImage
                    p.StringOption user.TextColor
                )
        }

    let save_new (user: User) : int64 =
        SAVE_NEW.ExecuteGetId user core_db |> expect

    let private BY_DISCORD_ID: Query<uint64, int64 * User> =
        {
            SQL = """SELECT * FROM users WHERE DiscordId = @DiscordId;"""
            Parameters = [ "@DiscordId", SqliteType.Text, -1 ]
            FillParameters = (fun p id -> p.String(string id))
            Read =
                (fun r ->
                    r.Int64,
                    {
                        Username = r.String
                        DiscordId = uint64 r.String
                        DateSignedUp = r.Int64
                        LastLogin = r.Int64
                        AuthToken = r.String
                        Badges = r.Json JSON
                        Color = r.Int32
                        Password = r.StringOption
                        ProfileBanner = r.String
                        ProfilePicture = r.String
                        CountryFlag = r.StringOption
                        Coins = r.Int64
                        PrimaryColor = r.StringOption
                        SecondaryColor = r.StringOption
                        AboutMe = r.StringOption
                        BackgroundImage = r.StringOption
                        TextColor = r.StringOption
                    }
                )
        }

    let by_discord_id (discord_id: uint64) =
        BY_DISCORD_ID.Execute discord_id core_db |> expect |> Array.tryExactlyOne

    let private BY_ID: Query<int64, User> =
        {
            SQL = """SELECT * FROM users WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8 ]
            FillParameters = (fun p id -> p.Int64 id)
            Read =
                (fun r ->
                    r.Int64 |> ignore

                    {
                        Username = r.String
                        DiscordId = uint64 r.String
                        DateSignedUp = r.Int64
                        LastLogin = r.Int64
                        AuthToken = r.String
                        Badges = r.Json JSON
                        Color = r.Int32
                        Password = r.StringOption
                        ProfileBanner = r.String
                        ProfilePicture = r.String
                        CountryFlag = r.StringOption
                        Coins = r.Int64
                        PrimaryColor = r.StringOption
                        SecondaryColor = r.StringOption
                        AboutMe = r.StringOption
                        BackgroundImage = r.StringOption
                        TextColor = r.StringOption
                    }
                )
        }

    let by_id (id: int64) =
        BY_ID.Execute id core_db |> expect |> Array.tryExactlyOne

    let by_ids (ids: int64 array) =
        if ids.Length = 0 then
            [||]
        else

        let ids_string = String.concat "," (Array.map string ids)

        let query: Query<unit, int64 * User> =
            { Query.without_parameters () with
                SQL = sprintf "SELECT * FROM users WHERE Id IN (%s)" ids_string
                Read =
                    (fun r ->
                        r.Int64,
                        {
                            Username = r.String
                            DiscordId = uint64 r.String
                            DateSignedUp = r.Int64
                            LastLogin = r.Int64
                            AuthToken = r.String
                            Badges = r.Json JSON
                            Color = r.Int32
                            Password = r.StringOption
                            ProfileBanner = r.String
                            ProfilePicture = r.String
                            CountryFlag = r.StringOption
                            Coins = r.Int64
                            PrimaryColor = r.StringOption
                            SecondaryColor = r.StringOption
                            AboutMe = r.StringOption
                            BackgroundImage = r.StringOption
                            TextColor = r.StringOption
                        }
                    )
            }

        query.Execute () core_db |> expect

    let private BY_AUTH_TOKEN: Query<string, int64 * User> =
        {
            SQL = """SELECT * FROM users WHERE AuthToken = @AuthToken;"""
            Parameters = [ "@AuthToken", SqliteType.Text, -1 ]
            FillParameters = (fun p token -> p.String token)
            Read =
                (fun r ->
                    r.Int64,
                    {
                        Username = r.String
                        DiscordId = uint64 r.String
                        DateSignedUp = r.Int64
                        LastLogin = r.Int64
                        AuthToken = r.String
                        Badges = r.Json JSON
                        Color = r.Int32
                        Password = r.StringOption
                        ProfileBanner = r.String
                        ProfilePicture = r.String
                        CountryFlag = r.StringOption
                        Coins = r.Int64
                        PrimaryColor = r.StringOption
                        SecondaryColor = r.StringOption
                        AboutMe = r.StringOption
                        BackgroundImage = r.StringOption
                        TextColor = r.StringOption
                    }
                )
        }

    let by_auth_token (token: string) =
        BY_AUTH_TOKEN.Execute token core_db |> expect |> Array.tryExactlyOne

    let private BY_USERNAME: Query<string, int64 * User> =
        {
            SQL = """SELECT * FROM users WHERE Username LIKE @Username ESCAPE '\';"""
            Parameters = [ "@Username", SqliteType.Text, -1 ]
            FillParameters = (fun p username -> p.String(username.Replace("_", "\\_")))
            Read =
                (fun r ->
                    r.Int64,
                    {
                        Username = r.String
                        DiscordId = uint64 r.String
                        DateSignedUp = r.Int64
                        LastLogin = r.Int64
                        AuthToken = r.String
                        Badges = r.Json JSON
                        Color = r.Int32
                        Password = r.StringOption
                        ProfileBanner = r.String
                        ProfilePicture = r.String
                        CountryFlag = r.StringOption
                        Coins = r.Int64
                        PrimaryColor = r.StringOption
                        SecondaryColor = r.StringOption
                        AboutMe = r.StringOption
                        BackgroundImage = r.StringOption
                        TextColor = r.StringOption
                    }
                )
        }

    let by_username (username: string) =
        BY_USERNAME.Execute username core_db |> expect |> Array.tryExactlyOne

    let private SEARCH_BY_USERNAME: Query<string, int64 * User> =
        {
            SQL = """SELECT * FROM users WHERE Username LIKE @Pattern ESCAPE '\' ORDER BY LastLogin DESC;"""
            Parameters = [ "@Pattern", SqliteType.Text, -1 ]
            FillParameters = (fun p query -> p.String("%" + query.Replace("_", "\\_") + "%"))
            Read =
                (fun r ->
                    r.Int64,
                    {
                        Username = r.String
                        DiscordId = uint64 r.String
                        DateSignedUp = r.Int64
                        LastLogin = r.Int64
                        AuthToken = r.String
                        Badges = r.Json JSON
                        Color = r.Int32
                        Password = r.StringOption
                        ProfileBanner = r.String
                        ProfilePicture = r.String
                        CountryFlag = r.StringOption
                        Coins = r.Int64
                        PrimaryColor = r.StringOption
                        SecondaryColor = r.StringOption
                        AboutMe = r.StringOption
                        BackgroundImage = r.StringOption
                        TextColor = r.StringOption
                    }
                )
        }

    let search_by_username (query: string) =
        SEARCH_BY_USERNAME.Execute query core_db |> expect

    let private LIST: Query<int, int64 * User> =
        {
            SQL = """SELECT * FROM users ORDER BY DateSignedUp ASC LIMIT @Limit OFFSET @Offset;"""
            Parameters = [ "@Limit", SqliteType.Integer, 8; "@Offset", SqliteType.Integer, 8 ]
            FillParameters =
                (fun p page ->
                    p.Int64 15L
                    p.Int64(int64 page * 15L)
                )
            Read =
                (fun r ->
                    r.Int64,
                    {
                        Username = r.String
                        DiscordId = uint64 r.String
                        DateSignedUp = r.Int64
                        LastLogin = r.Int64
                        AuthToken = r.String
                        Badges = r.Json JSON
                        Color = r.Int32
                        Password = r.StringOption
                        ProfileBanner = r.String
                        ProfilePicture = r.String
                        CountryFlag = r.StringOption
                        Coins = r.Int64
                        PrimaryColor = r.StringOption
                        SecondaryColor = r.StringOption
                        AboutMe = r.StringOption
                        BackgroundImage = r.StringOption
                        TextColor = r.StringOption
                    }
                )
        }

    let list (page: int) =
        if page < 0 then
            [||]
        else
            LIST.Execute page core_db |> expect

    let private SET_AUTH_TOKEN: NonQuery<int64 * string> =
        {
            SQL = """UPDATE users SET AuthToken = @AuthToken WHERE Id = @Id;"""
            Parameters = [ "@AuthToken", SqliteType.Text, -1; "@Id", SqliteType.Integer, 8 ]
            FillParameters =
                fun p (id, token) ->
                    p.String token
                    p.Int64 id
        }

    let set_auth_token (id: int64, token: string) =
        SET_AUTH_TOKEN.Execute (id, token) core_db |> expect |> ignore

    let private UPDATE_COLOR: NonQuery<int64 * int32> =
        {
            SQL = """UPDATE users SET Color = @Color WHERE Id = @Id;"""
            Parameters = [ "@Color", SqliteType.Integer, 4; "@Id", SqliteType.Integer, 8 ]
            FillParameters =
                fun p (id, color) ->
                    p.Int32 color
                    p.Int64 id
        }

    let update_color (id: int64, color: int32) =
        UPDATE_COLOR.Execute (id, color) core_db |> expect |> ignore

    let private UPDATE_BADGES: NonQuery<int64 * Set<Badge>> =
        {
            SQL = """UPDATE users SET Badges = @Badges WHERE Id = @Id;"""
            Parameters = [ "@Badges", SqliteType.Text, -1; "@Id", SqliteType.Integer, 8 ]
            FillParameters =
                fun p (id, badges) ->
                    p.Json JSON badges
                    p.Int64 id
        }

    let update_badges (id: int64, badges: Set<Badge>) =
        UPDATE_BADGES.Execute (id, badges) core_db |> expect |> ignore

    let private UPDATE_LAST_SEEN: NonQuery<int64> =
        {
            SQL = """UPDATE users SET LastLogin = @Now WHERE Id = @Id;"""
            Parameters = [ "@Now", SqliteType.Integer, 8; "@Id", SqliteType.Integer, 8 ]
            FillParameters =
                fun p id ->
                    p.Int64(Timestamp.now ())
                    p.Int64 id
        }

    let update_last_seen (id: int64) =
        UPDATE_LAST_SEEN.Execute id core_db |> expect |> ignore

    let private DELETE: NonQuery<int64> =
        {
            SQL = """DELETE FROM users WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8 ]
            FillParameters = fun p id -> p.Int64 id
        }

    let delete (id: int64) =
        DELETE.Execute id core_db |> expect |> ignore

    let private COUNT: Query<unit, int64> =
        { Query.without_parameters() with
            SQL = """SELECT COUNT(1) FROM users"""
            Read = fun r -> r.Int64
        }

    let count () : int64 =
        COUNT.Execute () core_db |> expect |> Array.exactlyOne

    let private RENAME: NonQuery<int64 * string> =
        {
            SQL = """UPDATE users SET Username = @Username WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8; "@Username", SqliteType.Text, -1 ]
            FillParameters =
                fun p (id, username) ->
                    p.Int64 id
                    p.String username
        }

    let rename (id: int64, new_name: string) =
        RENAME.Execute (id, new_name) core_db |> expect |> ignore
        
    let private UPDATE_BANNER: NonQuery<int64 * string> =
        {
            SQL = """UPDATE users SET ProfileBanner = @ProfileBanner WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8; "@ProfileBanner", SqliteType.Text, -1 ]
            FillParameters =
                fun p (id, profile_banner) ->
                    p.Int64 id
                    p.String profile_banner
        }
        
    let update_banner (id: int64, new_banner: string) =
        UPDATE_BANNER.Execute (id, new_banner) core_db |> expect |> ignore
    
    let private UPDATE_AVATAR: NonQuery<int64 * string> =
        {
            SQL = """UPDATE users SET ProfilePicture = @ProfilePicture WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8; "@ProfilePicture", SqliteType.Text, -1 ]
            FillParameters =
                fun p (id, profile_picture) ->
                    p.Int64 id
                    p.String profile_picture
        }
        
    let update_avatar (id: int64, new_avatar: string) =
        UPDATE_AVATAR.Execute (id, new_avatar) core_db |> expect |> ignore
        
    let private UPDATE_BG_IMAGE: NonQuery<int64 * string> =
        {
            SQL = """UPDATE users SET WebBackgroundImage = @WebBackgroundImage WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8; "@WebBackgroundImage", SqliteType.Text, -1 ]
            FillParameters =
                fun p (id, background_image) ->
                    p.Int64 id
                    p.String background_image
        }
        
    let update_bakground_image (id: int64, new_image: string) =
        UPDATE_BG_IMAGE.Execute (id, new_image) core_db |> expect |> ignore
    
    let private UPDATE_ABOUT_ME: NonQuery<int64 * string> =
        {
            SQL = """UPDATE users SET AboutMe = @AboutMe WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8; "@AboutMe", SqliteType.Text, -1 ]
            FillParameters =
                fun p (id, about_me) ->
                    p.Int64 id
                    p.String about_me
        }
        
    let update_about_me (id: int64, about_me: string) =
        UPDATE_BG_IMAGE.Execute (id, about_me) core_db |> expect |> ignore
    
    let private UPDATE_THEME: NonQuery<int64 * string * string * string> =
        {
            SQL = """UPDATE users SET PrimaryColor = @PrimaryColor, SecondaryColor = @SecondaryColor, TextColor = @TextColor WHERE Id = @Id;"""
            Parameters = [ "@Id", SqliteType.Integer, 8; "@PrimaryColor", SqliteType.Text, -1; "@SecondaryColor", SqliteType.Text, -1; "@TextColor", SqliteType.Text, -1 ]
            FillParameters =
                fun p (id, primary, secondary, text_color) ->
                    p.Int64 id
                    p.String primary
                    p.String secondary
                    p.String text_color
        }
        
    let update_theme (id: int64, primary: string, secondary: string, text_color: string) =
        UPDATE_THEME.Execute (id, primary, secondary, text_color) core_db |> expect |> ignore