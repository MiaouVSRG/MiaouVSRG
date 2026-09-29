namespace MiaouVSRG.CLI

open System.IO
open System.Diagnostics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

module Utils =

    type PathHelper() =
        static member Path([<CallerFilePath; Optional; DefaultParameterValue("")>] path: string) : string =
            Path.Combine(path, "..", "..") |> Path.GetFullPath

    let MIAOUVSRG_PATH = PathHelper.Path()
    let TOOLS_PATH = Path.Combine(MIAOUVSRG_PATH, "tools")
    let ASSETS_PATH = Path.Combine(MIAOUVSRG_PATH, "miaouvsrg", "assets")
    let SITE_PATH = Path.Combine(MIAOUVSRG_PATH, "site")

    let BUILD_RESOURCES_PATH =
        Path.Combine(MIAOUVSRG_PATH, "miaouvsrg", "src", "Resources")

    let MIAOUVSRG_SOURCE_PATH = Path.Combine(MIAOUVSRG_PATH, "miaouvsrg", "src")

    let exec (cmd: string) (args: string) =
        Process
            .Start(ProcessStartInfo(cmd, args, WorkingDirectory = MIAOUVSRG_PATH))
            .WaitForExit()

    let eval (cmd: string) (args: string) : string =
        let p =
            Process.Start(ProcessStartInfo(cmd, args, WorkingDirectory = MIAOUVSRG_PATH, RedirectStandardOutput = true))

        let output = p.StandardOutput.ReadToEnd()
        p.WaitForExit()
        output.Trim()

    let exec_at (path: string) (cmd: string) (args: string) =
        Process
            .Start(ProcessStartInfo(cmd, args, WorkingDirectory = path))
            .WaitForExit()

    let rec walk_fs_files (dir: string) : (string * string) seq =
        seq {
            for file in Directory.GetFiles(dir) do
                if Path.GetExtension(file).ToLower() = ".fs" then
                    yield file, File.ReadAllText file

            for dir in Directory.GetDirectories(dir) do
                let name = Path.GetFileName dir

                if name <> "bin" && name <> "obj" then
                    yield! walk_fs_files dir
        }