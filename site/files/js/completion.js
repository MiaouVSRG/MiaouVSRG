import { getApiEndpoint } from "./utils.js"

// Pour optimiser l'affichage, voici la logique de chatGPT :
// 1) On n'affiche que x éléments (définis par la variable VISIBLE_COUNT) sur la page
// 2) Quand on scrolle, on regarde de combien de maps on a scrollé (comme on sait qu'une map fait 45px (défini dans le completion.css), on peut facilement calculer que si on a scroll de 90px, alors on a scroll 2 maps)
// 3) Pour que la scrollbar soit "infinie" (sinon on serait bloqué), on crée une div invisible avec une hauteur égale au nombre de maps qu'on a scrollé (donc si on scoll 2 maps, alors on a une div invisible de 90px de hauteur, ça permet de rallonger la scrollbar et d'éviter d'être bloqué)

// Pour faire le tri, voici la logique de chatGPT :
// 1) Dans le HTML, on crée une div avec l'id "sortBar", qui contient elle même les différents éléments que l'on veut trier (donc ici une div "Title", une div "DifficultyName", etc.)
// 2) Dans le JS, on va ajouter un event à la div "sortBar" qui se déclenchera à chaque fois que l'on va cliquer dessus (oui, visiblement on peut faire ça sur des divs et pas que sur des boutons, mb)
// 2b) Cet event va regarder si l'utilisateur a cliqué sur "title", "difficulty name", etc, et va donc appliquer le tri en fonction (si l'utiisateur a recliqué sur ce qu'il voulait trier, alors le tri s'applique dans l'ordre décroissant)

let data = []; // réponse API

let currentSort = { key: "Title", asc: true };
let filteredData = [];
let defaultData = [];

// Star rating completion stats
let currentStarRatingStatsOptions = { source: "Total", keymode: 4}

let userResponse = {}

const container = document.getElementById("listContainer");
const completionCalculatorNumber = document.getElementById("completioncalculatornumber");

const ITEM_HEIGHT = 45;
const VISIBLE_COUNT = 30;

let startIndex = 0;


// Cette fonction permet de convertir un temps en minutes en secondes. Par exemple, elle convertit "6m30s" en "390"
function parseLength(length) {
    // split("m") permet de séparer le texte à chaque fois que le caractère "m" est présent (donc "6m30" va devenir "6" "30")
    // const [m, s] dit à JS que la première valeur (donc "6") est la variable m, et que la seconde valeur (donc "30") est la variable s
    const [m, s] = length.replace("s", "").split("m");

    // Ensuite on fait le calcul tout bête : 60*m + s (pour avoir le nombre total de secondes)
    return parseInt(m) * 60 + parseInt(s);
}

function sortData() {
    const { key, asc } = currentSort;

    filteredData.sort((a, b) => {
        let valA = a.ChartInfo[key];
        let valB = b.ChartInfo[key];

        if (key === "Length") {
            valA = parseLength(valA);
            valB = parseLength(valB);
        }

        if (typeof valA === "string") {
            return asc ? valA.localeCompare(valB) : valB.localeCompare(valA);
        }

        return asc ? valA - valB : valB - valA;
    });
}

function applyKeyModeFilter(){
    const selectedKeymodes = [...document.querySelectorAll(".keymodefilter input:checked")]
        .map(cb => parseInt(cb.parentElement.textContent));

    // applyKeyModeFilter is the first function called in applyFilters, so we need to apply the filter to the base maps, and not the already filtered maps
    filteredData = defaultData.filter(item =>
        selectedKeymodes.includes(item.ChartInfo.Keymode)
    );
}

function applySourceFilter(){
    const selectedSources = [...document.querySelectorAll(".gamefilter input:checked")]
        .map(cb => {
            switch(cb.parentElement.textContent){
                case "Osu!":
                    return "osu!";
                case "BMS":
                    return "BMS";
                case "O2Jam":
                    return "O2Jam"
            }
        });

    // Apply to filtered data because this function is called after applyKeyModeFilter, so there is already a filter made
    filteredData = filteredData.filter(item =>
        selectedSources.includes(item.ChartInfo.Source)
    );
}

function applyClearedFilter(){
    const selectedClears = [...document.querySelectorAll(".clearfilter input:checked")]
        .map(cb => cb.parentElement.textContent);

    let clearcmos = selectedClears.includes("show cleared");
    let clearcmosNON = selectedClears.includes("show uncleared");

    if(clearcmos && !clearcmosNON){
        filteredData = filteredData.filter(item =>
            item.Passed
        );
    }

    if(!clearcmos && clearcmosNON){
        filteredData = filteredData.filter(item =>
            !item.Passed
        );
    }
}

function applyStarRatingFilter(){
    const minval = document.getElementById("minsr").value;
    const maxval = document.getElementById("maxsr").value;

    filteredData = filteredData.filter(item =>
        item.ChartInfo.Difficulty >= minval && item.ChartInfo.Difficulty <= maxval
    );
}

function applySearchBarFilter(){
    const searchval = document.getElementById("name").value;

    if(searchval !== ""){
        filteredData = filteredData.filter(item => 
            item.ChartInfo.Title.toLowerCase().includes(searchval.toLowerCase()) || item.ChartInfo.DifficultyName.toLowerCase().includes(searchval.toLowerCase())
        );
    }
}

function setMapsCounter(){
    const total_item_count = filteredData.length;
    const passed_item_count = filteredData.filter(item =>
        item.Passed
    ).length;

    completionCalculatorNumber.textContent = passed_item_count + "/" + total_item_count;
}
 
// Fonction appellée à chaque fois qu'une checkbox est cochée.
// Elle remet tous les filtres, et applique le tri sur les maps filtrées
function applyFilters() {

    applyKeyModeFilter();
    applySourceFilter();
    applyClearedFilter();
    applyStarRatingFilter();
    applySearchBarFilter();

    setMapsCounter();

    sortData();

    startIndex = 0;
}


function render() {
    container.innerHTML = "";

    const fragment = document.createDocumentFragment();

    const start = startIndex;
    const end = Math.min(
        //startIndex + VISIBLE_COUNT + 10 * 2,
        //sortedData.length
        startIndex + VISIBLE_COUNT + 10 * 2,
        filteredData.length

    );

    // 🔥 espace au-dessus
    const topSpacer = document.createElement("div");
    topSpacer.style.height = (start * ITEM_HEIGHT) + "px";
    fragment.appendChild(topSpacer);

    // 🔥 items visibles
    for (let i = start; i < end; i++) {
        //const item = sortedData[i];
        const item = filteredData[i];

        const div = document.createElement("div");
        div.className = "completioncard";

        div.innerHTML = `
            <img class="mapbg" src="${item.ChartInfo.Background || "/assets/images/marchepa.png"}">
            <div class="mapname"><a style="color: white; text-decoration: none;" target="blank" href="/charts/chartpage/${item.ChartInfo.ChartId}">${item.ChartInfo.Title}</a></div>
            <div class="mapdiffname">${item.ChartInfo.DifficultyName}</div>
            <div class="mapkeymode">${item.ChartInfo.Keymode+"k"}</div>
            <div class="maprating">${item.ChartInfo.Difficulty.toFixed(2)+"🟆"}</div>
            <div class="maplenght">${item.ChartInfo.Length}</div>
            <div class="maplink">
            <a href="${item.ChartInfo.DownloadLink}"><img class="downloadbuttonimg" src="/assets/images/downloadmapbutton.png"></a>
            </div>
        `;

        if (item.Passed) div.classList.add("passed");

        fragment.appendChild(div);
    }

    // 🔥 espace en dessous
    const bottomSpacer = document.createElement("div");
    bottomSpacer.style.height =
        //((sortedData.length - end) * ITEM_HEIGHT) + "px";
        ((filteredData.length - end) * ITEM_HEIGHT) + "px";

    fragment.appendChild(bottomSpacer);

    container.appendChild(fragment);
}

function setMapRatioIndividual(event){
    let parentElement;
    if(event.classList.contains("completionstats2")){
        parentElement = event;
    } else {
        parentElement = event.parentElement;
    }

    const diffRangeMin = parseInt(parentElement.children[0].textContent.split("-")[0]);
    const diffRangeMax = diffRangeMin + 1;

    const {source, keymode} = currentStarRatingStatsOptions;

    const filteredBySource = 
        source === "Total" 
            ? defaultData 
            : defaultData.filter(item =>
                item.ChartInfo.Source === source
            );

    const filteredBySourceAndKeymode = filteredBySource.filter(item =>
        item.ChartInfo.Keymode === keymode
    );

    const filteredByStarRating = filteredBySourceAndKeymode.filter(item =>
        item.ChartInfo.Difficulty >= diffRangeMin && item.ChartInfo.Difficulty <= diffRangeMax
    );

    const passedMaps = filteredByStarRating.filter(item => 
        item.Passed
    ).length;

    const totalMaps = filteredByStarRating.length;

    parentElement.children[2].textContent = passedMaps + "/" + totalMaps;
}

function setMapRatioGame(event){
    if(defaultData.length === 0) return;
    if(event.classList.contains("completionstatsspan")) return;

    let parentElement;
    let authorizedClassNames = ["totalcompletionstats", "osucompletionstats", "bmscompletionstats", "o2jamcompletionstats"];

    if(event.classList.value.split(" ").some(el => authorizedClassNames.includes(el))){
        parentElement = event;
    } else {
        parentElement = event.parentElement;
    }

    let completionSpan = parentElement.children[1];
    let source = getSourceById(completionSpan.id)

    let maps = source === "Total" 
    ? defaultData 
    : defaultData.filter(item => 
        item.ChartInfo.Source === source
    );

    let mapcount = maps.length;
    let passedMapcount = maps.filter(item => 
        item.Passed
    ).length;

    completionSpan.innerText = passedMapcount + "/" + mapcount;
}

function getSourceById(id){
    if(id.includes("osu")) return "osu!";
    if(id.includes("bms")) return "BMS";
    if(id.includes("o2jam")) return "O2Jam";
    if(id.includes("total")) return "Total";
    return;
}

let ticking = false;

window.addEventListener("scroll", () => {
    const scrollTop = window.scrollY;

    const newStart = Math.max(
        0,
        Math.floor(scrollTop / ITEM_HEIGHT) - 10
    );

    if (newStart !== startIndex) {
        startIndex = newStart;
        render();
    }
});

// tri click
document.getElementById("sortBar").addEventListener("click", (e) => {
    const key = e.target.dataset.sort;
    if (!key) return;

    if (currentSort.key === key) {
        currentSort.asc = !currentSort.asc;
    } else {
        currentSort = { key, asc: true };
    }

    sortData();
    render();
});

document.querySelectorAll(".clearfilter input").forEach(checkbox => {
    checkbox.addEventListener("change", () => {
        applyFilters();
        render();
    });
});

document.querySelectorAll(".keymodefilter input").forEach(checkbox => {
    checkbox.addEventListener("change", () => {
        applyFilters();
        render();
    });
});

document.querySelector(".resetfilterbutton").addEventListener("click", () => {

    document.querySelectorAll(".keymodefilter input")
        .forEach(cb => cb.checked = true);

    applyFilters();
    render();
});

document.querySelectorAll(".gamefilter input").forEach(checkbox => {
    checkbox.addEventListener("change", () => {
        applyFilters();
        render();
    });
});

document.querySelector(".resetfilterbutton2").addEventListener("click", () => {

    document.querySelectorAll(".gamefilter input")
        .forEach(cb => cb.checked = true);

    applyFilters();
    render();
});

const statspannel = document.getElementById("statspannel").children;
for(var i = 0; i < statspannel.length; i++){
    statspannel[i].addEventListener("mouseover", (event) => setMapRatioGame(event.target));
    statspannel[i].addEventListener("mouseout", () => setGameCompletionValues(userResponse));
}

document.querySelectorAll(".statspannel2 .completionstats2").forEach(element => {
    element.addEventListener("mouseover", (event) => setMapRatioIndividual(event.target))
});

document.querySelectorAll(".statspannel2 .completionstats2").forEach(element => {
    element.addEventListener("mouseout", (event) => applyStarRatingStats())
});

document.getElementById("name").addEventListener("input", () => {
    applyFilters();
    render();
});

document.getElementById("minsr").addEventListener("change", () => {
    applyFilters();
    render();
});

document.getElementById("maxsr").addEventListener("change", () => {
    applyFilters();
    render();
});

let usercharts = document.getElementById("usercharts");

usercharts.addEventListener("change", () => {
    currentStarRatingStatsOptions.source = usercharts.value

    // Automatically set the keymode to 7 if O2Jam or BMS
    if(usercharts.value === "BMS" || usercharts.value === "O2Jam"){
        userkeymode.selectedIndex = 3;
        disableAllKeymodesSelectionExcept(7);
        currentStarRatingStatsOptions.keymode = 7;
    } else {
        enableAllKeymodesSelection();
    }
    applyStarRatingStats();
});

let userkeymode = document.getElementById("userkeymode");
userkeymode.addEventListener("change", () => {
    currentStarRatingStatsOptions.keymode = parseInt(userkeymode.value);
    applyStarRatingStats();
});

function disableAllKeymodesSelectionExcept(key){
    const children = userkeymode.children;
    for(i = 0; i < children.length; i++){
        const child = children[i];
        if(parseInt(child.value) !== key){
            child.disabled = true;
        }
    }
}

function enableAllKeymodesSelection(){
    const children = userkeymode.children;
    for(i = 0; i < children.length; i++){
        const child = children[i];
        child.disabled = false;
    }
}

// simulation fetch
async function init(response) {
    data = await response.json();
    let loadingImage = document.getElementById("loadingimage");
    let loadingText = document.getElementById("loadingspan");
    loadingImage.style.display = "none";
    loadingText.style.display = "none";




    //sortedData = [...data];

    //sortData();
    //render();
    defaultData = [...data];
    currentStarRatingStatsOptions.source = usercharts.value
    currentStarRatingStatsOptions.keymode = parseInt(userkeymode.value);

    sortData();
    applyFilters();
    applyStarRatingStats();
    render();
}






// ===== RÉCUPÉRATION DU BOUTON DANS LE DOM =====
// On récupère le bouton dans une variable pour pouvoir l'utiliser en JS
const jumpToTopBtn = document.getElementById("jumpToTopBtn");

// ===== FONCTION POUR FAIRE DÉFILER VERS LE HAUT =====
// Cette fonction utilise la méthode window.scrollTo pour remonter en haut de la page
// Le comportement "smooth" permet un défilement fluide
function scrollToTop() {
    window.scrollTo({
        top: 0, // Position verticale : 0 (tout en haut)
        behavior: "smooth" // Animation fluide
    });
}

// ===== AFFICHER/MASQUER LE BOUTON EN FONCTION DU SCROLL =====
// On écoute l'événement "scroll" sur la fenêtre
window.addEventListener("scroll", function() {
    // Si l'utilisateur a scrolled de plus de 100px, on affiche le bouton
    if (window.pageYOffset > 300) {
        jumpToTopBtn.style.display = "block"; // Affiche le bouton
    } else {
        jumpToTopBtn.style.display = "none"; // Masque le bouton
    }
});

// ===== ÉCOUTEUR D'ÉVÉNEMENT POUR LE CLIQUE SUR LE BOUTON =====
// Quand on clique sur le bouton, on appelle la fonction scrollToTop
jumpToTopBtn.addEventListener("click", scrollToTop);


function setGameCompletionValues(response){
    if(response.ProfileInfo){
        var htmlcompletion=document.getElementById("totalcompletion")
        htmlcompletion.innerText=response.ProfileInfo.StatsGlobal.Completion

        var htmlcompletion=document.getElementById("totalcompletionbar")
        htmlcompletion.value=response.ProfileInfo.StatsGlobal.Completion.replace("%","")



        var htmlcompletion=document.getElementById("osucompletion")
        htmlcompletion.innerText=response.ProfileInfo.OsuCompletion

        var htmlcompletion=document.getElementById("osucompletionbar")
        htmlcompletion.value=response.ProfileInfo.OsuCompletion.replace("%","")



        var htmlcompletion=document.getElementById("bmscompletion")
        htmlcompletion.innerText=response.ProfileInfo.BMSCompletion

        var htmlcompletion=document.getElementById("bmscompletionbar")
        htmlcompletion.value=response.ProfileInfo.BMSCompletion.replace("%","")



        var htmlcompletion=document.getElementById("o2jamcompletion")
        htmlcompletion.innerText=response.ProfileInfo.O2JamCompletion

        var htmlcompletion=document.getElementById("o2jamcompletionbar")
        htmlcompletion.value=response.ProfileInfo.O2JamCompletion.replace("%","")
    }
}

function applyStarRatingStats(){
    const {source, keymode} = currentStarRatingStatsOptions;

    const filteredBySource = 
        source === "Total" 
            ? defaultData 
            : defaultData.filter(item =>
                item.ChartInfo.Source === source
            );

    const filteredBySourceAndKeymode = filteredBySource.filter(item =>
        item.ChartInfo.Keymode === keymode
    );

    const completionBars = document.getElementsByClassName("completionbar2");
    const percentSpans = document.getElementsByClassName("percentspan2");

    for(i = 0; i < completionBars.length; i++){
        let completionbar = completionBars[i];
        let percentspan = percentSpans[i];
        const maps = filteredBySourceAndKeymode.filter(item =>
            // difficulty between i and i + 1 (so between 0 and 1, 1 and 2 etc.)
            item.ChartInfo.Difficulty <= i + 1 && item.ChartInfo.Difficulty >= i
        );

        const passedMaps = maps.filter(item =>
            item.Passed
        );

        // Avoid divided by 0 errors which result in NaN
        const ratio = maps.length === 0 ? 0 : (passedMaps.length / maps.length) * 100;

        completionbar.value = ratio;
        percentspan.innerText = Number((ratio).toFixed(0)) + "%";
    }
}

window.onload = async (event) => {
    const parts = location.pathname.split("/").filter(Boolean);
    let username = parts.at(-1);
    let page = parts.at(-2);

    if(username && page === "completion"){

        const response = await fetch(getApiEndpoint() + "/web/user/completion?name=" + username);

        init(response)

        const userResponse = await fetch(getApiEndpoint() + "/web/user?name=" + username);
        const userJson = await userResponse.json();
        userResponse = userJson;
        setGameCompletionValues(userResponse);
    } else {

        fetch(getApiEndpoint() + "/web/login/verify", {
            method: "GET",
            credentials: "include"
        })
        .then((response) => response.json())
        .then((json) => {
            if (json.Success){
                fetch(getApiEndpoint() + "/web/user/completion", {
                    method: "GET",
                    credentials: "include"
                })
                .then((response) => init(response))

                fetch(getApiEndpoint() + "/web/user", {
                    method: "GET",
                    credentials: "include"
                })
                .then((response) => response.json())
                .then((json) => {
                    userResponse = json
                    setGameCompletionValues(userResponse)
                })
            } else {
            }
        });
    }
}