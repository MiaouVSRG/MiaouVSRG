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

    // following svg (when user follows someone who dont followed back)
    const followingIcon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    followingIcon.classList.add("svg");
    followingIcon.classList.add("follow");
    followingIcon.setAttribute("viewBox", "0 0 24 24");
    followingIcon.setAttribute("fill", "currentColor");
    const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    path.setAttribute("d", "M14.9999 15.2547C13.8661 14.4638 12.4872 14 10.9999 14C7.40399 14 4.44136 16.7114 4.04498 20.2013C4.01693 20.4483 4.0029 20.5718 4.05221 20.6911C4.09256 20.7886 4.1799 20.8864 4.2723 20.9375C4.38522 21 4.52346 21 4.79992 21H9.94465M13.9999 19.2857L15.7999 21L19.9999 17M14.9999 7C14.9999 9.20914 13.2091 11 10.9999 11C8.79078 11 6.99992 9.20914 6.99992 7C6.99992 4.79086 8.79078 3 10.9999 3C13.2091 3 14.9999 4.79086 14.9999 7Z");
    path.setAttribute("stroke", "#37FF00");
    path.setAttribute("stroke-width", "3");
    path.setAttribute("stroke-linecap", "round");
    path.setAttribute("stroke-linejoin", "round");
    g.appendChild(path);
    followingIcon.appendChild(g);

    // mutual svg (when both players follow each other)
    const mutualIcon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    mutualIcon.classList.add("svg");
    mutualIcon.classList.add("mutual");
    mutualIcon.setAttribute("viewBox", "0 0 24 24");
    mutualIcon.setAttribute("fill", "currentColor");
    const gMutual = document.createElementNS('http://www.w3.org/2000/svg', 'g');
    const pathMutual = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    pathMutual.setAttribute("d", "M15.7 4C18.87 4 21 6.98 21 9.76C21 15.39 12.16 20 12 20C11.84 20 3 15.39 3 9.76C3 6.98 5.13 4 8.3 4C10.12 4 11.31 4.91 12 5.71C12.69 4.91 13.88 4 15.7 4Z");
    pathMutual.setAttribute("stroke", "#FF56D5");
    pathMutual.setAttribute("stroke-width", "3");
    pathMutual.setAttribute("stroke-linecap", "round");
    pathMutual.setAttribute("stroke-linejoin", "round");
    gMutual.appendChild(pathMutual);
    mutualIcon.appendChild(gMutual);

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

    const svgFollowingOrMutual = friendJson.IsMutual ? mutualIcon : followingIcon;
    userCard.appendChild(svgFollowingOrMutual);

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