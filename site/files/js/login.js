const discordLogin = document.getElementById("discordlogin");

const form = document.getElementById("userform");

form.addEventListener("submit", (e) => {
    e.preventDefault();
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    if(username === "" || password === ""){
        return;
    }

    if(e.submitter.value === "register"){
        fetch("https://api.miaou.dev.internal/web/user/register", {
            method: "POST",
            body: JSON.stringify({
                Username: username,
                Password: password,
            })
        })
        .then((response) => response.json())
        .then((json) => console.log(json));
    } else if (e.submitter.value === "login"){
        fetch("https://api.miaou.dev.internal/web/user/login", {
            method: "POST",
            body: JSON.stringify({
                Username: username,
                Password: password,
            })
        })
        .then((response) => response.json())
        .then((json) => console.log(json));
    } else {
        return;
    }
});

discordLogin.addEventListener("click", () => {
    window.location.href = "https://api.miaou.dev.internal/web/login/discord";
});

window.onload = (event) => {
    fetch("https://api.miaou.dev.internal/web/login/verify", {
            method: "GET",
            credentials: "include"
        })
        .then((response) => response.json())
        .then((json) => {
            if (json.Success){
                window.location.href = "/user/profile"
            }
        });
};