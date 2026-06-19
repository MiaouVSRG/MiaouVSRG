namespace Interlude.Features.Online

open System
open System.IO
open System.Diagnostics
open System.Reflection
open System.Runtime.InteropServices
open Percyqaz.Common
open System.IO.Compression
open Percyqaz.Data
open Prelude
open Prelude.Data
open Prelude.Data.Library.Imports

module Updates =
    
    let credentials = Credentials.Load()

    /// Numeric version e.g. "0.5.16"
    let short_version : string =
        let v = Assembly.GetExecutingAssembly().GetName()

        if v.Version.Revision <> 0 then
            v.Version.ToString(4)
        else
            v.Version.ToString(3)

    /// Github commit SHA
    let short_hash : string =
        let informational_version =
            Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion

        let hash = informational_version.Substring(informational_version.IndexOf('+') + 1)
        hash.Substring(0, min hash.Length 6)

    /// Full version string e.g. "Interlude 0.5.16"
    let version : string =
        let v = Assembly.GetExecutingAssembly().GetName()

        if DEV_MODE then
            sprintf "%s %s (%s)" v.Name short_version short_hash
        else
            sprintf "%s %s" v.Name short_version

    let private get_interlude_location () : string =
        Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)

    // this doesn't just copy a folder to a destination, but renames any existing/duplicates of the same name to .old
    let rec private swap_update_files (source: string) (dest: string) (swap_login_file: bool) : unit =

        Logging.Debug $"Swapping update files from %s{source} to %s{dest}"

        if swap_login_file then
            // Copy login file to prevent data loss
            let login_file = Path.Combine(dest, "Data", "login.json")
            Logging.Debug "%s %s" (Path.Combine(source, "login.json")) login_file
            try
                Directory.CreateDirectory (Path.Combine(source, "Data")) |> ignore
                File.Copy(login_file, Path.Combine(source, "Data", "login.json"), true)
                File.Move(login_file, Path.Combine(source, "Data", "login.json.old"), true)
            with
            | :? FileNotFoundException -> ()
            | other -> Logging.Error $"Error while moving login.json file during auto-update: {other}"
        
        
        Directory.EnumerateFiles source
        |> Seq.iter (fun source ->
            let target = Path.Combine(dest, Path.GetFileName source)
            try
                File.Move(target, source + ".old", true)
            with
            | :? FileNotFoundException -> ()
            | other -> Logging.Error $"Error while moving file '%s{source}' during auto-update: {other}"
            File.Copy(source, target, true)
        )

        Directory.EnumerateDirectories source
        |> Seq.iter (fun d ->
            let targetd = Path.Combine(dest, Path.GetFileName d)
            Directory.CreateDirectory targetd |> ignore
            swap_update_files d targetd false
        )

    [<Json.AutoCodec>]
    type GithubAsset =
        {
            name: string
            browser_download_url: string
            updated_at: string
        }

    [<Json.AutoCodec>]
    type GithubRelease =
        {
            url: string
            tag_name: string
            name: string
            published_at: string
            body: string
            assets: GithubAsset list
            prerelease: bool
        }

    let mutable restart_on_exit = false

    let mutable latest_version_name = "<Unknown, server could not be reached>"
    let mutable latest_release = None
    let mutable update_available = false
    let mutable update_started = false
    let mutable update_complete = false

    let asset_name =
        match RuntimeInformation.OSArchitecture with
        | Architecture.X64 when OperatingSystem.IsWindows() -> Ok "MiaouVSRG.zip"
        | Architecture.X64 when OperatingSystem.IsLinux() -> Ok "MiaouVSRG-linux-x64.zip"
        | other -> Error other
        
    let private get_release_information (releases: GithubRelease list) : bool * GithubRelease =
        let is_user_in_beta_channel = credentials.Channel.ToLower() = "beta"
        
        // We first check if there is a beta version available
        // Beta version <=> pre-release, and the pre-release is always the second release visible on the API
        let is_beta = is_user_in_beta_channel && releases[1].prerelease
        let release = if is_beta then releases[1] else releases[0]
        
        is_beta, release

    let private handle_update (releases: GithubRelease list) : unit =
        let is_beta, release = get_release_information releases
        
        latest_release <- Some release

        let parse_version (s: string) =
            let s = s.Split(".")

            if s.Length > 3 then
                (int s[0], int s[1], int s[2], int s[3])
            else
                (int s[0], int s[1], int s[2], 0)

        let current = short_version
        
        let tag_name = release.tag_name
        
        // Possible tags : "MiaouVSRG-v0.x.x.x" or "MiaouVSRG-v0.x.x.xb", where "b" stands for beta
        let incoming = tag_name.Replace("MiaouVSRG-", "").Replace("b", "").Substring(1)
        latest_version_name <- incoming

        let pcurrent = parse_version current
        let pincoming = parse_version incoming
        
        let is_new_update = pincoming >= pcurrent && release.assets[0].updated_at <> credentials.LastTimeUpdated

        match asset_name with
        | Error arch -> Logging.Info "Auto-updater doesn't support this OS or architecture (%O)" arch
        | Ok _ ->

        if is_beta && (credentials.Channel.ToLower() <> "beta") && is_new_update then
            Logging.Info "Beta update available, but user is on Stable channel"
        elif is_new_update then
            Logging.Info "Update available (%s)!" incoming
            update_available <- true
        elif pincoming < pcurrent then
            Logging.Debug "Current build (%s) is ahead of update stream (%s)." current incoming
        else
            Logging.Info "Game is up to date."

    let check_for_updates () : unit =
        WebServices.download_json (
            "https://api.github.com/repos/MiaouVSRG/MiaouVSRG/releases",
            function
            | WebResult.Ok(d: GithubRelease list) -> handle_update d
            | _ -> ()
        )

        let path = get_interlude_location ()
        let folder_path = Path.Combine(path, "update")

        Imports.delete_folder.Request(folder_path, ignore)

    let apply_update (progress: float32 -> unit, callback: unit -> unit) : unit =
        if not update_available then
            failwith "No update available to install"

        if update_started then
            ()
        else

            update_started <- true
            
            credentials.LastTimeUpdated <- latest_release.Value.assets[0].updated_at
            credentials.Save()

            match
                latest_release.Value.assets
                |> List.tryFind (fun asset -> Ok asset.name = asset_name)
            with
            | None ->
                Logging.Error(
                    "Update failed: The github release doesn't have a download for your platform. Report this as a bug!"
                )
            | Some asset ->

            let download_url = asset.browser_download_url
            let path = get_interlude_location ()
            let zip_path = Path.Combine(path, "update.zip")
            let folder_path = Path.Combine(path, "update")
            File.Delete zip_path

            WebServices.download_file.Request(
                (download_url, zip_path, progress),
                fun success ->
                    if success then
                        ZipFile.ExtractToDirectory(zip_path, folder_path)
                        Imports.delete_file.Request(zip_path, ignore)
                        swap_update_files folder_path path true
                        callback ()
                        update_complete <- true
            )