namespace MiaouVSRG.Web.Tests.Client

open MiaouVSRG.Web.Shared

type MalformedPacketsSession() =
    inherit FuzzSession("malformed_packet", ignore)

    override this.OnHandshaked() =
        this.SendRaw([|0x00uy; 0x01uy; 0x00uy; 14uy|])