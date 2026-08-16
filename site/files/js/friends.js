import { getApiEndpoint } from "/js/utils.js";

const innerBox = document.getElementById("innerbox");

// response : Friend array
// Friend : {Avatar, Banner, Country (can be null), IsOnline, Username}
function init(response){
    response.forEach(friend => {
        makeFriendCard(friend);
    });
}

function makeFriendCard(friendJson){
    const userCard = document.createElement("div");
    userCard.classList.add("usercard");
    const img = document.createElement("img");
    img.classList.add("usercardbg");
    img.src = friendJson.Banner;

    userCard.appendChild(img);

    const flag = document.createElement("span");
    const userCountry = friendJson.Country ?? "xx";
    const flagclass = userCountry.toLowerCase() === "xx" ? "unknownflag" : "fi-" + userCountry;
    flag.classList.add("fis", "fi", flagclass, "userflag");

    userCard.appendChild(flag);

    const userPfp = document.createElement("div");
    userPfp.classList.add("userpfp");

    const userPfpImg = document.createElement("img");
    userPfpImg.classList.add("userpfpimg");
    userPfpImg.src = friendJson.Avatar;

    userPfpImg.onclick = () => {
        window.open("/user/profile/" + friendJson.Username, "_blank");
    }

    userPfp.appendChild(userPfpImg);

    userCard.appendChild(userPfp);

    const username = document.createElement("div");
    username.classList.add("username");
    const usernamespan = document.createElement("span");
    usernamespan.classList.add("usernamespan");
    usernamespan.innerText = friendJson.Username;

    usernamespan.onclick = () => {
        window.open("/user/profile/" + friendJson.Username, "_blank");
    }

    username.appendChild(usernamespan);

    userCard.appendChild(username);

    const status = document.createElement("div");
    status.classList.add("userstatus");
    const statusdot = document.createElement("div");
    statusdot.classList.add("statusdot", friendJson.IsOnline ? "online" : "offline");
    status.appendChild(statusdot);
    const statustextspan = document.createElement("span");
    statustextspan.innerText = friendJson.IsOnline ? "online" : "offline";

    status.appendChild(statustextspan);
    userCard.appendChild(status);

    innerBox.appendChild(userCard);
}


window.onload = (event) => {
    fetch(getApiEndpoint() + "/web/login/verify", {
        method: "GET",
        credentials: "include"
    })
    .then((response) => response.json())
    .then((json) => {
        if (json.Success){
            fetch(getApiEndpoint() + "/web/user/friends", {
                method: "GET",
                credentials: "include"
            })
            .then((response) => response.json())
            .then((json) => init(json, true))
        } else {
        }
    });
}