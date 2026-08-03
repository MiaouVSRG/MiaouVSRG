namespace Interlude.Web.Shared

open System
open System.Web
open System.Net
open Interlude.Web.Shared
open NetCoreServer
open System.Net.Http
open System.Net.Sockets
open System.Diagnostics
open Percyqaz.Common
open Prelude

[<AutoOpen>]
module HttpResponseExtensions =
    
    let ALLOWED_ORIGINS =
        set [
            // .dev.internal : for local testing, not impacting production
            "https://api.miaou.dev.internal"
            "https://miaou.dev.internal"
            
            // Main website
            "https://miaouvsrg.com"
            "https://api.miaouvsrg.com"
            
            // Beta website for testing
            "https://www.beta.miaouvsrg.com"
            "https://beta.api.miaouvsrg.com"
        ]

    type HttpResponse with
        member this.ReplyJson<'T>(data: 'T, ?code: int, ?cookies: (string * string * int option * string) array, ?origin: string) =
            this.Clear()
                .SetBegin(if code.IsSome then code.Value else 200) |> ignore
               
            if cookies.IsSome && cookies.Value <> null then
                for cookie_name, cookie_value, max_age, domain in cookies.Value do
                    if max_age.IsSome then
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/; Max-Age={max_age.Value};") |> ignore
                    else
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/") |> ignore
                 
            if origin.IsSome && ALLOWED_ORIGINS.Contains(origin.Value) then
                this
                    .SetHeader("Access-Control-Allow-Origin", origin.Value)
                    .SetHeader("Access-Control-Allow-Credentials", "true") |> ignore
            else
                this
                    .SetHeader("Access-Control-Allow-Origin", "*") |> ignore
                
            this
                .SetHeader("Content-Type", "application/json")
                .SetBody(JSON.ToString data)
            |> ignore

        member this.ReplyRedirect(url: string, ?cookies: (string * string * int option * string) array, ?origin: string) =
            this.Clear()
                .SetBegin(303) |> ignore
                
            if cookies.IsSome && cookies.Value <> null then
                for cookie_name, cookie_value, max_age, domain in cookies.Value do
                    if max_age.IsSome then
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/; Max-Age={max_age.Value};") |> ignore
                    else
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/") |> ignore
            
            if origin.IsSome && ALLOWED_ORIGINS.Contains(origin.Value) then
                this
                    .SetHeader("Access-Control-Allow-Origin", origin.Value)
                    .SetHeader("Access-Control-Allow-Credentials", "true") |> ignore
            else
                this
                    .SetHeader("Access-Control-Allow-Origin", "*") |> ignore
                    
            this
                .SetHeader("Location", url)
                .SetBody()
            |> ignore
            
        member this.ReplyFile(file: byte array, filename: string, ?cookies: (string * string * int option * string) array, ?origin: string) =
            this.Clear()
                .SetBegin(200) |> ignore
                
            if cookies.IsSome && cookies.Value <> null then
                for cookie_name, cookie_value, max_age, domain in cookies.Value do
                    if max_age.IsSome then
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/; Max-Age={max_age.Value};") |> ignore
                    else
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/") |> ignore
           
            if origin.IsSome && ALLOWED_ORIGINS.Contains(origin.Value) then
                this
                    .SetHeader("Access-Control-Allow-Origin", origin.Value)
                    .SetHeader("Access-Control-Allow-Credentials", "true") |> ignore
            else
                this
                    .SetHeader("Access-Control-Allow-Origin", "*") |> ignore
                    
            this
                .SetHeader("Content-Type", "application/octet-stream")
                .SetHeader("Content-Disposition", $"attachment; filename=\"{filename}\"")
                .SetBody(file)
            |> ignore

        member this.ReplyError(code: int, reason: string, ?cookies: (string * string * int option * string) array, ?origin: string) =
            this.Clear()
                .SetBegin(code) |> ignore
                
            if cookies.IsSome && cookies.Value <> null then
                for cookie_name, cookie_value, max_age, domain in cookies.Value do
                    if max_age.IsSome then
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/; Max-Age={max_age.Value};") |> ignore
                    else
                        this.SetHeader("Set-Cookie", $"{cookie_name}={cookie_value}; Domain={domain}; HttpOnly; Secure; SameSite=None; Path=/") |> ignore
            
            if origin.IsSome && ALLOWED_ORIGINS.Contains(origin.Value) then
                this
                    .SetHeader("Access-Control-Allow-Origin", origin.Value)
                    .SetHeader("Access-Control-Allow-Credentials", "true") |> ignore
            else
                this
                    .SetHeader("Access-Control-Allow-Origin", "*") |> ignore
                    
            this
                .SetHeader("Cache-Control", "no-cache, no-store")
                .SetHeader("Content-Type", "text/plain; charset=UTF-8")
                .SetBody(reason)
            |> ignore
            
        /// This method is used for web browsers OPTIONS request (sent before any POST request).
        /// We authorize the browser to send forms and files
        member this.ReplyOptions() =
            this.Clear()
                .SetBegin(200)
                .SetHeader("Access-Control-Allow-Origin", "*")
                .SetHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
                .SetHeader("Access-Control-Allow-Headers", "Content-Type, Authorization")
            |> ignore

module API =

    let escape : string -> string = Uri.EscapeDataString

    type HttpMethod =
        | GET
        | POST
        | DELETE

    type Config =
        {
            Port: int
            SSLContext: SslContext
            Handle_Request:
                HttpMethod * string * string * byte array * Map<string, string array> * Map<string, string> * HttpResponse
                    -> Async<unit>
        }

    let base_uri = Uri("https://localhost/")

    type private Session(server: HttpsServer, config: Config) =
        inherit HttpsSession(server)

        override this.OnReceivedRequest(request: HttpRequest) =
            let uri = new Uri(base_uri, request.Url)

            let query_params =
                let from_uri = HttpUtility.ParseQueryString uri.Query

                seq {
                    for p in from_uri do
                        yield (p, from_uri.GetValues p)
                }
                |> Map.ofSeq

            let headers =
                seq {
                    for i = 0 to int request.Headers - 1 do
                        let struct (key, value) = request.Header i in (key, value)
                }
                |> Map.ofSeq

            try
                let before = Stopwatch.GetTimestamp()
                match request.Method with
                | "GET" ->
                    config.Handle_Request(GET, uri.AbsolutePath, request.Body, request.BodyBytes, query_params, headers, this.Response)
                    |> Async.RunSynchronously
                | "DELETE" ->
                    config.Handle_Request(DELETE, uri.AbsolutePath, request.Body, request.BodyBytes, query_params, headers, this.Response)
                    |> Async.RunSynchronously
                | "POST" ->
                    config.Handle_Request(POST, uri.AbsolutePath, request.Body, request.BodyBytes, query_params, headers, this.Response)
                    |> Async.RunSynchronously
                | "OPTIONS" -> this.Response.ReplyOptions()
                | _ -> this.Response.ReplyError(404, "Not found") |> ignore

                Logging.Info "%s %s responded %i in %.0fms" request.Method uri.AbsolutePath this.Response.Status (Stopwatch.GetElapsedTime(before).TotalMilliseconds)

                this.SendResponseAsync this.Response |> ignore
            with e ->
                Logging.Critical "Error handling HTTP request %O: %O" request e

        override this.OnReceivedRequestError(request: HttpRequest, error: string) =
            Logging.Error "Error handling HTTP request: %s" error

        override this.OnError(error: SocketError) =
            if error <> SocketError.NotConnected then
                Logging.Error "Socket error in HTTP session %O: %O" this.Id error

    type private Listener(config: Config) =
        inherit HttpsServer(config.SSLContext, IPAddress.Any, config.Port)

        override this.CreateSession() = new Session(this, config)

        override this.OnError(error: SocketError) =
            Logging.Error "Error in HTTP server: %O" error

    module Server =

        let mutable private server = Unchecked.defaultof<Listener>

        let init (config: Config) = server <- new Listener(config)

        let start () =
            if server.Start() then
                Logging.Info "HTTP server is listening!"

        let stop () =
            if server.Stop() then
                Logging.Info "Stopped HTTP server."

    module Client =

        let http_client_handler = new HttpClientHandler()
        let private client = new HttpClient(http_client_handler)

        let init (base_address: string) =
            client.BaseAddress <- new Uri(base_address)
            client.Timeout <- TimeSpan.FromSeconds(5.0)

        let private queue =
            { new Async.Queue<HttpClient -> Async<unit>, unit>() with
                override this.Handle(action) = async { do! action client }
            }

        let authenticate (token: string) =
            client.DefaultRequestHeaders.Authorization <- new Headers.AuthenticationHeaderValue("Bearer", token)

        let rec private _send_retry (retries_left: int) (client: HttpClient) (message: HttpRequestMessage)  =
            async {
                let! response = client.SendAsync message |> Async.AwaitTask
                if response.StatusCode = HttpStatusCode.Unauthorized && retries_left > 0 then
                    Threading.Thread.Sleep(50)
                    return! _send_retry (retries_left - 1) client (new HttpRequestMessage(message.Method, message.RequestUri, Content = message.Content))
                else
                    return response
            }

        let send_retry = _send_retry 3

        let internal get<'T> (route: string, callback: 'T option -> unit) : unit =

            let handle_response (response: HttpResponseMessage) =
                if response.IsSuccessStatusCode then
                    match response.Content.ReadAsStream() |> fun s -> JSON.FromStream(route, s) with
                    | Ok res -> callback (Some res)
                    | Error err ->
                        Logging.Error "Error getting %s: %s" route err.Message
                        callback None
                else
                    callback None

            queue.Request(
                fun client ->
                    async {
                        try
                            let request = new HttpRequestMessage(HttpMethod.Get, route)
                            let! response = send_retry client request
                            handle_response response
                        with
                        | :? HttpRequestException
                        | :? OperationCanceledException
                        | :? AggregateException -> callback None
                    }
                , ignore
            )

        let internal get_async<'T> (route: string, callback: 'T option -> unit) : Async<unit> =

            let handle_response (response: HttpResponseMessage) =
                if response.IsSuccessStatusCode then
                    match response.Content.ReadAsStream() |> fun s -> JSON.FromStream(route, s) with
                    | Ok res -> callback (Some res)
                    | Error err ->
                        Logging.Error "Error getting %s: %s" route err.Message
                        callback None
                else
                    callback None

            queue.RequestAsync(
                fun client ->
                    async {
                        try
                            let request = new HttpRequestMessage(HttpMethod.Get, route)
                            let! response = send_retry client request
                            handle_response response
                        with
                        | :? HttpRequestException
                        | :? OperationCanceledException
                        | :? AggregateException -> callback None
                    }
            )

        let internal post_return<'T, 'U> (route: string, payload: 'T, callback: 'U option -> unit) : unit =
            let handle_response (response: HttpResponseMessage) =
                if response.IsSuccessStatusCode then
                    match response.Content.ReadAsStream() |> fun s -> JSON.FromStream(route, s) with
                    | Ok res -> callback (Some res)
                    | Error err ->
                        callback None
                else
                    callback None

            queue.Request(
                fun client ->
                    async {
                        try
                            let request = new HttpRequestMessage(HttpMethod.Post, route)
                            request.Content <-
                                new StringContent(
                                    JSON.ToString payload,
                                    Text.Encoding.UTF8,
                                    "application/json"
                                )
                            let! response = send_retry client request
                            handle_response response
                        with
                        | :? AggregateException -> callback None
                    }
                , ignore
            )

        let internal post<'T> (route: string, payload: 'T, callback: bool option -> unit) : unit =
            post_return<'T, bool> (route, payload, callback)

        let internal post_async<'T, 'U> (route: string, payload: 'T, callback: 'U option -> unit) : Async<unit> =

            let handle_response (response: HttpResponseMessage) =
                if response.IsSuccessStatusCode then
                    match response.Content.ReadAsStream() |> fun s -> JSON.FromStream(route, s) with
                    | Ok res -> callback (Some res)
                    | Error err ->
                        Logging.Error "Error reading post %s: %s" route err.Message
                        callback None
                else
                    callback None

            queue.RequestAsync(
                fun client ->
                    async {
                        try
                            let request = new HttpRequestMessage(HttpMethod.Post, route)
                            request.Content <-
                                new StringContent(
                                    JSON.ToString payload,
                                    Text.Encoding.UTF8,
                                    "application/json"
                                )
                            let! response = send_retry client request
                            handle_response response
                        with
                        | :? HttpRequestException
                        | :? OperationCanceledException
                        | :? AggregateException -> callback None
                    }
            )

        let internal delete (route: string, callback: bool option -> unit) : unit =

            let handle_response (response: HttpResponseMessage) =
                if response.IsSuccessStatusCode then
                    match response.Content.ReadAsStream() |> fun s -> JSON.FromStream(route, s) with
                    | Ok res -> callback (Some res)
                    | Error err ->
                        Logging.Error "Error reading delete %s: %s" route err.Message
                        callback None
                else
                    callback None

            queue.Request(
                fun client ->
                    async {
                        try
                            let request = new HttpRequestMessage(HttpMethod.Delete, route)
                            let! response = send_retry client request
                            handle_response response
                        with
                        | :? HttpRequestException
                        | :? OperationCanceledException
                        | :? AggregateException -> callback None
                    }
                , ignore
            )