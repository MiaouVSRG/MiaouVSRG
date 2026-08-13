import { getApiEndpoint } from "./utils.js"

let params = new URLSearchParams(document.location.search);
let token = params.get("token");

fetch(getApiEndpoint() + "/web/login/validate?token=" + token.replace("+", "%2B"), {
    method: "GET",
    credentials: "include"
})
.then((response) => response.json())
.then((json) => {
    if (json.Success){
        window.location.href = "/user/profile"
    }
});