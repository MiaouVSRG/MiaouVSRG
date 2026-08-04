import { getApiEndpoint } from "./utils.js"

const leaderboardinnerbox = document.getElementById("leaderboardinnerbox");

function init(response){
    let rank = 1;
    response.forEach(player => {
        const playercard = document.createElement("div");
        playercard.classList.add("playercard");

        const rankDiv = document.createElement("div");
        rankDiv.classList.add("rank");
        switch(rank){
            case 1:
                rankDiv.classList.add("rgb");
                break;
            case 2:
                rankDiv.classList.add("gold");
                break;
            case 3:
                rankDiv.classList.add("silver");
                break;
            case 4:
                rankDiv.classList.add("bronze");
                break;
            default:
                break;
        }
        rankDiv.innerText = "#" + rank;

        playercard.appendChild(rankDiv);

        const country = document.createElement("div");
        country.classList.add("country");
        const countryflag = document.createElement("span");
        countryflag.classList.add("flagradius");
        countryflag.classList.add("fi");
        countryflag.classList.add("fi-" + player.Country);
        country.appendChild(countryflag);

        playercard.appendChild(country);

        const username = document.createElement("div");
        username.classList.add("username");
        const usernameLink = document.createElement("a");
        usernameLink.href = "/user/profile/" + player.Username;
        usernameLink.target = "_blank";
        usernameLink.innerText = player.Username;

        username.appendChild(usernameLink);

        playercard.appendChild(username);

        const accuracy = document.createElement("div");
        accuracy.classList.add("accuracy");
        accuracy.innerText = Number(player.Accuracy).toFixed(2);

        playercard.appendChild(accuracy);

        const playcount = document.createElement("div");
        playcount.classList.add("playcount");
        playcount.innerText = player.Playcount;

        playercard.appendChild(playcount);

        const rating = document.createElement("div");
        rating.classList.add("rating");
        rating.innerText = Number(player.Rating).toFixed(2);

        playercard.appendChild(rating);

        leaderboardinnerbox.appendChild(playercard);
        
        rank++;
    });

}

window.onload = (event) => {
    fetch(getApiEndpoint() + "/web/leaderboard", {
        method: "GET"
        // no need to "credentials: include" as we do not need token for this request 
    })
    .then((response) => response.json())
    .then((json) => init(json))
}