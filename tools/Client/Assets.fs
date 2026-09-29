namespace MiaouVSRG.CLI.Features

open System.IO
open System.IO.Compression
open Catnip.Skins.Noteskins
open MiaouVSRG.CLI

module Assets =

    let private make_zip (source: string) (target_zip: string) =
        printfn "Making zip: %s" target_zip

        if File.Exists target_zip then
            File.Delete target_zip

        ZipFile.CreateFromDirectory(source, target_zip)

    let private cleanup_noteskin_json (id) =
        match Path.Combine(Utils.ASSETS_PATH, id) |> Noteskin.FromPath with
        | Ok ns ->
            ns.Config <- ns.Config
        | Error err -> raise err

    let bundle_assets () =
        make_zip
        <| Path.Combine(Utils.ASSETS_PATH, "default")
        <| Path.Combine(Utils.BUILD_RESOURCES_PATH, "default.zip")

        cleanup_noteskin_json "miaoum"

        make_zip
        <| Path.Combine(Utils.ASSETS_PATH, "miaoum")
        <| Path.Combine(Utils.BUILD_RESOURCES_PATH, "miaoum.zip")
