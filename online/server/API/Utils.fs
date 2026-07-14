namespace Interlude.Web.Server.API

open System
open NetCoreServer
open Prelude
open Interlude.Web.Server.Domain.Core

[<AutoOpen>]
module Utils =

    exception NotAuthorizedException
    exception NotFoundException
    exception AuthorizeFailedException
    exception PermissionDeniedException
    exception BadRequestException of Message: string option

    let BEARER_LENGTH = "Bearer ".Length
    
    let parseCookies (cookieHeader: string) : Map<string, string> =
        cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries)
        |> Seq.choose (fun cookie ->
            match cookie.Trim().Split('=', 2) with
            | [| name; value |] ->
                Some(name.Trim(), value.Trim())
            | _ ->
                None
        )
        |> Map.ofSeq

    let authorize (header: Map<string, string>) =

        if header.ContainsKey("Authorization") then
            let auth_header = header.["Authorization"]
            if auth_header.Length > BEARER_LENGTH then
                match User.by_auth_token (header.["Authorization"].Substring(BEARER_LENGTH)) with
                | Some(id, user) -> id, user
                | None -> raise AuthorizeFailedException
            else raise AuthorizeFailedException

        else
            raise NotAuthorizedException

    let require_query_parameter (query_params: Map<string, string array>) (name: string) =
        if not (query_params.ContainsKey name) then
            raise (BadRequestException(Some(sprintf "'%s' is required" name)))
            
    let require_cookie (header: Map<string, string>) (cookie_name: string) =
        if not (header.ContainsKey("Cookie")) || not (header["Cookie"].Contains(cookie_name)) then
            raise (BadRequestException(Some($"Cookie {cookie_name} is required")))
            
    let require_referer (header: Map<string, string>) (website: string) =
        if not (header.ContainsKey("Referer")) || not (header["Referer"].Contains(website)) then
            raise (BadRequestException(Some($"Referer {website} is required")))
            
    let get_cookie (header: Map<string, string>) (cookie_name: string) =
        let cookies =
            header
            |> Map.tryFind "Cookie"
            |> Option.map parseCookies
            |> Option.defaultValue Map.empty
            
        cookies[cookie_name]