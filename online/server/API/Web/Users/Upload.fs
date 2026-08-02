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
    
    let verify_image (image_bytes: byte array) =
        use ms = new MemoryStream(image_bytes)
        use image = Image.Load(ms)
        
        // Banners or pfps should not be bigger than 2048 in height or width
        if image.Width > 2048 || image.Height > 2048 then
            Error "Image dimensions too large"
        else
            Ok image
    
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
                match verify_image body with
                | Error err -> response.ReplyError(400, err)
                | Ok image ->
                    let picture_type = query_params["type"][0]
                    if picture_type = "banner" then
                        use file = File.Create($"./banners/{id}.png")
                        image.SaveAsPng(file)
                        User.update_banner (id, $"https://cdn.miaouvsrg.com/banners/{id}")
                        let res: Response = { Success = true }
                        response.ReplyJson(res)
                        
                    elif picture_type = "avatar" then
                        use file = File.Create($"./avatars/{id}.png")
                        image.SaveAsPng(file)
                        User.update_avatar (id, $"https://cdn.miaouvsrg.com/avatars/{id}")
                        let res: Response = { Success = true }
                        response.ReplyJson(res)
                    else
                        response.ReplyError(400, "Invalid picture type parameter")
        }
