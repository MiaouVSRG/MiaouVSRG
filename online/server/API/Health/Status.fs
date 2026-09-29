namespace MiaouVSRG.Web.Server.API.Health

open NetCoreServer
open MiaouVSRG.Web.Shared
open MiaouVSRG.Web.Shared.Requests
open MiaouVSRG.Web.Server.API

module Status =

    open Health.Status

    let mutable private request_counter = 0

    let handle
        (
            body: string,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        async {
            request_counter <- request_counter + 1

            response.ReplyJson(
                {
                    Status =
                        sprintf
                            "Everything will be ok. This endpoint has been called %i times since last restart."
                            request_counter
                }
                : Response
            )
        }