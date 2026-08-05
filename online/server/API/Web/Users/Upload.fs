namespace Interlude.Web.Server.API.Web.Users

open System.IO
open Interlude.Web.Server.API
open Interlude.Web.Server.Domain.Core
open Interlude.Web.Shared
open Interlude.Web.Shared.Requests.Web.User.Upload
open NetCoreServer
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

module Upload =
    
    let verify_and_save_image (image_bytes: byte array, path: string) =
        use ms = new MemoryStream(image_bytes)
        use image = Image.Load(ms)
        
        // Banners or pfps should not be bigger than 2048 in height or width
        if image.Width > 2048 || image.Height > 2048 then
            Error "Image dimensions too large"
        else
            use file = File.Create(path)
            image.SaveAsPng(file)
            Ok true
    
    let handle
        (
            body: byte array,
            query_params: Map<string, string array>,
            headers: Map<string, string>,
            response: HttpResponse
        ) =
        
        async{
            let token = require_cookie headers "token"
            let id, _ = authorize_with_cookie token
            require_query_parameter query_params "type"
            
            // Avoid too large files
            if body.Length > 5 * 1024 * 1024 then
                response.ReplyError(400, "File too large")
            else
                let picture_type = query_params["type"][0]
                if picture_type = "banner" then
                    match verify_and_save_image (body, $"./banners/{id}.png") with
                    | Error err -> response.ReplyError(400, err)
                    | Ok _ -> 
                        User.update_banner (id, $"https://cdn.miaouvsrg.com/banners/{id}.png")
                        let res: Response = { Success = true }
                        response.ReplyJson(res, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
                        
                elif picture_type = "avatar" then
                    match verify_and_save_image (body, $"./avatars/{id}.png") with
                    | Error err -> response.ReplyError(400, err)
                    | Ok _ -> 
                        User.update_avatar (id, $"https://cdn.miaouvsrg.com/avatars/{id}.png")
                        let res: Response = { Success = true }
                        response.ReplyJson(res, 200, Unchecked.defaultof<(string * string * int option * string) array>, headers["Origin"])
                else
                    response.ReplyError(400, "Invalid picture type parameter")
        }
