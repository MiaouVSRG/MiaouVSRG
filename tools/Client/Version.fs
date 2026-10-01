namespace MiaouVSRG.CLI.Features

open System.IO
open MiaouVSRG.CLI.Utils

module Version =

    let mutable private current_version =
        let file = Path.Combine(MIAOUVSRG_SOURCE_PATH, "MiaouVSRG.fsproj")
        let f = File.ReadAllText file

        let i = f.IndexOf "<AssemblyVersion>"
        let j = f.IndexOf "</AssemblyVersion>"

        f.Substring(i, j - i).Substring("<AssemblyVersion>".Length)

    let publish () =

        let changelog = Path.Combine(MIAOUVSRG_PATH, "miaouvsrg", "docs", "changelog.md")
        let logtxt = File.ReadAllText(changelog)
        let latest = logtxt.Split(current_version + "\r\n" + "====", 2).[0]

        if latest.Trim() = "" then
            failwithf "No changelog for new version. Create this first"

        let v = latest.Split("====", 2).[0].Trim()
        File.WriteAllText(Path.Combine(MIAOUVSRG_PATH, "miaouvsrg", "docs", "changelog-latest.md"), latest)

        printfn "Version: %s -> %s" current_version v
        printfn "%s" latest
        let file = Path.Combine(MIAOUVSRG_SOURCE_PATH, "MiaouVSRG.fsproj")
        let mutable f = File.ReadAllText file

        do
            let i = f.IndexOf "<AssemblyVersion>"
            let j = f.IndexOf "</AssemblyVersion>"

            f <- f.Substring(0, i) + "<AssemblyVersion>" + v + f.Substring(j)

        do
            let i = f.IndexOf "<FileVersion>"
            let j = f.IndexOf "</FileVersion>"

            f <- f.Substring(0, i) + "<FileVersion>" + v + f.Substring(j)

        File.WriteAllText(file, f)

        current_version <- v
        Site.generate_index v

        printfn "Creating git commit"

        exec "git" (sprintf "commit -a -m \"🏷️ Version %s\"" v)

    let version () = current_version