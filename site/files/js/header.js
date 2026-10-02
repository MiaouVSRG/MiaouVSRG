import { getApiEndpoint } from "./utils.js";

var headerInitialized = false;

const difficulties = ['beginner', 'easy', 'normal', 'hard', 'expert', 'master'];
const types = ['hourly', 'daily', 'weekly', 'monthly'];

async function waitForHeaderToInitialize(){
    return new Promise((resolve) => {
        var interval = 
            setInterval(() => {
                console.log("waiting");
                if(headerInitialized){
                    clearInterval(interval);
                    resolve("resolved");
                }
            });
    });
}

function switchTypeBack(){
    const type = document.getElementById("challtypespan");
    const index = types.indexOf(type.innerText) === 0 ? types.length : types.indexOf(type.innerText);
    const newType = types[index - 1];
    type.innerText = newType;
}

function switchTypeNext(){
    const type = document.getElementById("challtypespan");
    const index = types.indexOf(type.innerText) === types.length - 1 ? -1 : types.indexOf(type.innerText);
    const newType = types[index + 1];
    type.innerText = newType;
}

function switchDifficultyBack(){
    const diff = document.getElementById("challdiffspan");
    const index = difficulties.indexOf(diff.innerText) === 0 ? difficulties.length : difficulties.indexOf(diff.innerText);
    const newDiff = difficulties[index - 1];
    const challdiffbox = document.getElementById("challdiffbox");
    challdiffbox.classList.remove(diff.innerText);
    challdiffbox.classList.add(newDiff);
    diff.innerText = newDiff;
}

function switchDifficultyNext(){
    const diff = document.getElementById("challdiffspan");
    const index = difficulties.indexOf(diff.innerText) === difficulties.length - 1 ? -1 : difficulties.indexOf(diff.innerText);
    const newDiff = difficulties[index + 1];
    const challdiffbox = document.getElementById("challdiffbox");
    challdiffbox.classList.remove(diff.innerText);
    challdiffbox.classList.add(newDiff);
    diff.innerText = newDiff;
}

function createChallengeRatingCard(challenge){
    `
    <div class="challcard">
        <span class="challsizediv">get 7+ rating on x maps</span>
        <span class="rewardonly"> 1,000$</span>
    </div>
    `
}

function createChallengeAccuracyCard(challenge){
    if(!!challenge.AccuracyChart){
        return `
        <div class="challcard">
            <span class="challsizediv">get 90%+ on x maps >5✦</span>
            <span class="rewardonly"> 1,000$</span>
        </div>
        `
    }
}

function createChallengeChartCard(challenge){
    return `
    <div class="challcard">
        <div class="namediffchall">
            <span class="mapchallname">${challenge.Name}</span>
            <span class="challsr">[6.80✦] (c pa vrai g oublié)</span>
        </div>
            <div class="challacc">>${Number(challenge.ChallengeChart.GoalAccuracy ?? 0.85).toFixed(2) * 100}%</div>
            <span class="reward">${challenge.Coins}$</span>
    </div>
    `
}

function createChallengesDiv(challenges){
    // ${
    //         challenges
    //         .filter(challenge => challenge.ChallengeChart)
    //         .map(challenge => createChallengeChartCard(challenge))
    //         .join("")
    //     }
    return `
    <div class="challengesbox">
        <div class="challbg ">

        <div class="challkeybox">

            <span class="challkeyspan">4k</span>
            <span class="challkeyspan">5k</span>
            <span class="challkeyspan">6k</span>
            <span class="challkeyspan">7k</span>
            <span class="challkeyspan">8k</span>
            <span class="challkeyspan">9k</span>
            <span class="challkeyspan">10k</span>


        </div>

        <div class="challdiffbox normal" id="challdiffbox">
            <div class="switch" id="switch-difficulty-back"><</div>
            <span class="challdiffspan" id="challdiffspan">normal</span>
            <div class="switch" id="switch-difficulty-next">></div>
        </div>

        <div class="challtypebox" id="challtypebox">
            <div class="switch" id="switch-type-back"><</div>
            <span class="challtypespan" id="challtypespan">daily</span>
            <div class="switch" id="switch-type-next">></div>
        </div>

        </div>


    </div>
    `
}

// Ce fichier a pour but de représenter tous les éléments html du header, pour n'avoir qu'à importer ce fichier si on a besoin de mettre le header.
window.addEventListener("DOMContentLoaded", async () => {
    // const challReq = await fetch(getApiEndpoint() + "/challenge/ongoing");
    // const challenges = await challReq.json();
    // console.log(
    //     challenges
    // )
    const challenges = []

    const headerDiv = document.createElement("header");
    headerDiv.innerHTML = 
    `
    <div class="headerfix">
        <div class="header">
            <img class="pipheader" src="/favicon.png">
            <div class="homebutton">home</div>
            <div class="lbbutton">leaderboards</div>
            <div class="challengebutton">challenges</div>
            <div class="shopbutton">shop</div>
            <div class="completionbutton">completion</div>
            <svg fill="#ffffff" height="34px" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <g id="SVGRepo_bgCarrier" stroke-width="0"></g>
                <g id="SVGRepo_tracerCarrier" stroke-linecap="round" stroke-linejoin="round"></g>
                <g id="SVGRepo_iconCarrier">
                    <path
                            d="M20.207 18.793L16.6 15.184a7.027 7.027 0 1 0-1.416 1.416l3.609 3.609a1 1 0 0 0 1.414-1.416zM6 11a5 5 0 1 1 5 5 5.006 5.006 0 0 1-5-5z">
                    </path>
                </g>
            </svg>
            <div id="headerusername" class="headerusername">username</div>
            <img id="headerpfp" class="headerpfp" src="/assets/images/placeholder/PFP%20PLACEHOLDER.png">
        </div>
    </div>
    
    <div class="miniprofilebox">
        <span class="miniprofileprofile">profile</span>
        <span class="miniprofilefriends">friends</span>
        <span class="miniprofilesettings">settings</span>
        <span class="miniprofilelogout">log out</span>
    </div>

    ${createChallengesDiv(challenges)}
    `

    document.body.appendChild(headerDiv);
    document.getElementById("switch-difficulty-back").addEventListener("click", switchDifficultyBack);
    document.getElementById("switch-difficulty-next").addEventListener("click", switchDifficultyNext);
    document.getElementById("switch-type-back").addEventListener("click", switchTypeBack);
    document.getElementById("switch-type-next").addEventListener("click", switchTypeNext);
    headerInitialized = true;
});

export {waitForHeaderToInitialize}