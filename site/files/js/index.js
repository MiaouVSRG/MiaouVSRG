import { getApiEndpoint } from "./utils.js"

const topscoresBox = document.getElementById("topscores-box");
const topscoresMainBox = document.getElementById("topscores-mainbox");
const showMoreButton = document.getElementById("showmorebutton");
const showMoreText = document.getElementById("showmoretext");
const keymodefilterTopPlays = document.getElementById("keymodefiltertopplays");
const topscoresNumberofmaps = document.getElementById("topscores-numberofmaps");
const headerpfp = document.getElementById("headerpfp");
const headerusername = document.getElementById("headerusername");

const avatar = document.getElementById("avatarimg");
const banner = document.getElementById("bannerimg");

// settings box
const settingsBox = document.getElementById("settings");
const avatarPreviewImg = document.getElementById("previewpfp");
const avatarUploadButton = document.getElementById("avatar-upload");
const bannerPreviewImg = document.getElementById("previewbanner")
const bannerUploadButton = document.getElementById("banner-upload");
const backgroundPreviewImg = document.getElementById("previewbg");
const backgroundUploadButton = document.getElementById("background-upload");
const primaryColorPicker = document.getElementById("colorpickermain");
const secondaryColorPicker = document.getElementById("colorpickersecondary");
const alphaPrimaryValueSpan = document.getElementById("alphaprimaryvalue");
const alphaSecondaryValueSpan = document.getElementById("alphasecondaryvalue");
const alphaPrimaryInput = document.getElementById("alphaprimary");
const alphaSecondaryInput = document.getElementById("alphasecondary");

var backgroundImage = "";

// main constants
const body = document.getElementsByTagName('body')[0];

function isImageValid(filename){
    return filename.endsWith(".png") || filename.endsWith(".jpeg") || filename.endsWith(".jpg") || filename.endsWith(".webp");
}

function init(response, isCurrentUser){
    headerpfp.src = response.ProfileInfo.Avatar;
    headerusername.innerText = response.ProfileInfo.Username;

    backgroundImage = response.ProfileInfo.BackgroundImage;
    body.style.backgroundImage = "url(" + backgroundImage + ")";

    primaryColorPicker.value = response.ProfileInfo.PrimaryColor;
    secondaryColorPicker.value = response.ProfileInfo.SecondaryColor;

    setColorTheme(response.ProfileInfo.PrimaryColor, response.ProfileInfo.SecondaryColor);

    if(isCurrentUser){
        // Pour fermer la popup des settings
        document.getElementById("backbutton").onclick = () => {
            settingsBox.close();
        };

        document.getElementById("applybutton").onclick = () => {
            submitImage();
            submitTheme();
        };

        // Pour ouvrir la popup des settings
        avatar.onclick = () => {
            settingsBox.showModal();
        };

        banner.onclick = () => {
            settingsBox.showModal();
        }

        avatarUploadButton.onchange = () => {
            if(isImageValid(avatarUploadButton.files[0].name)){
                avatarPreviewImg.src = URL.createObjectURL(avatarUploadButton.files[0]);
            }
        };

        bannerUploadButton.onchange = () => {
            if(isImageValid(bannerUploadButton.files[0].name)){
                bannerPreviewImg.src = URL.createObjectURL(bannerUploadButton.files[0]);
            }
        };

        backgroundUploadButton.onchange = () => {
            if(isImageValid(backgroundUploadButton.files[0].name)){
                backgroundPreviewImg.src = URL.createObjectURL(backgroundUploadButton.files[0]);
            }
        };

        alphaPrimaryInput.onchange = () => {
            alphaPrimaryValueSpan.innerText = "Opacity: " + alphaPrimaryInput.value;
        };

        alphaSecondaryInput.onchange = () => {
            const val = Math.round(alphaSecondaryInput.value * 255).toString(16)
            console.log(val);
            alphaSecondaryValueSpan.innerText = "Opacity: " + val;
        };
    }



    // "document" représente la page html, "getElementById" permet de récupérer un élément du "document" via son id
    var htmlusername=document.getElementById("username")

    // Remplacer le texte par le Username renvoyé par le serveur
    htmlusername.innerText=response.ProfileInfo.Username





    //main stats
    var htmllevel=document.getElementById("level")
    htmllevel.innerText="lv."+response.ProfileInfo.Level

    var htmlfollowers=document.getElementById("followers")
    htmlfollowers.innerHTML=response.ProfileInfo.Followers

    var htmlglobalranking=document.getElementById("globalranking")
    htmlglobalranking.innerText="#" + response.ProfileInfo.StatsGlobal.GlobalRanking

    var htmlcountryranking=document.getElementById("countryranking")
    htmlcountryranking.innerText="#" + response.ProfileInfo.StatsGlobal.CountryRanking

    var htmlplayerrating=document.getElementById("playerrating")
    htmlplayerrating.innerText=Number(response.ProfileInfo.StatsGlobal.PlayerRating).toFixed(2);

    var htmlplaytime=document.getElementById("playtime")
    htmlplaytime.innerText=response.ProfileInfo.Playtime

    var htmlcompletion=document.getElementById("completion")
    htmlcompletion.innerText=response.ProfileInfo.StatsGlobal.Completion




    //grades
    var htmllevel=document.getElementById("normalpass")
    htmllevel.innerText=response.ProfileInfo.GradeCount.Normal.Pass

    var htmllevel=document.getElementById("normalclear")
    htmllevel.innerText=response.ProfileInfo.GradeCount.Normal.Clear

    var htmllevel=document.getElementById("normalclearplus")
    htmllevel.innerText=response.ProfileInfo.GradeCount.Normal.ClearPlus

    var htmllevel=document.getElementById("normaloverclear")
    htmllevel.innerText=response.ProfileInfo.GradeCount.Normal.Overclear

    var htmllevel=document.getElementById("normaloverclearplus")
    htmllevel.innerText=response.ProfileInfo.GradeCount.Normal.OverclearPlus

    var htmllevel=document.getElementById("normalperfect")
    htmllevel.innerText=response.ProfileInfo.GradeCount.Normal.Perfect




    //other stats
    var htmllevel=document.getElementById("hitaccuracy")
    htmllevel.innerText=response.ProfileInfo.HitAccuracy

    var htmllevel=document.getElementById("playcount")
    htmllevel.innerText=response.ProfileInfo.Playcount

    var htmllevel=document.getElementById("totalhits")
    htmllevel.innerText=response.ProfileInfo.TotalHits

    var htmllevel=document.getElementById("totalscore")
    htmllevel.innerText=response.ProfileInfo.TotalScore

    var htmllevel=document.getElementById("osutotalcompletion")
    htmllevel.innerText=response.ProfileInfo.OsuCompletion

    var htmllevel=document.getElementById("o2jamtotalcompletion")
    htmllevel.innerText=response.ProfileInfo.O2JamCompletion

    var htmllevel=document.getElementById("bmstotalcompletion")
    htmllevel.innerText=response.ProfileInfo.BMSCompletion

    var htmllevel=document.getElementById("etternatotalcompletion")
    htmllevel.innerText=response.ProfileInfo.EtternaCompletion




    //pfp and banner
    avatar.src=response.ProfileInfo.Avatar;

    banner.src=response.ProfileInfo.Banner;





    //select

    // On récupère les valeurs que l'on souhaite modifier en fonction de ce qu'a mis l'utilisateur
    // (Change par les id que tu as mis dans ton html)
    var global_ranking_value = document.getElementById("globalranking")
    var country_ranking_value = document.getElementById("countryranking")
    var playerrating = document.getElementById("playerrating")
    var playtime = document.getElementById("playtime")

    // On récupère le select du html
    var keymodefilter = document.getElementById("keymodefilter")

    // Voici une fonction que l'on a nommé "changeValues".
    // On l'appellera automatique à chaque fois qu'une valeur différente sera choisie dans le select par l'utilisateur
    // Tout le code à l'intérieur de cette fonction sera alors exécuté
    function changeValues(){
        // On récupère la champ "value" de l'option choisie par l'utilisateur
        // Donc, par rapport à mon HTML, si l'utilisateur choisit "All keymodes", alors la variable value sera égale à "global"
        var value = keymodefilter.value;
        // Si la value est égale à "global"
        if(value === "global"){
            // Alors le texte de la page doit valoir ce qui est contenu dans response.ProfileInfo.StatsGlobal
            global_ranking_value.innerText ="#" + response.ProfileInfo.StatsGlobal.GlobalRanking
            country_ranking_value.innerText ="#" + response.ProfileInfo.StatsGlobal.CountryRanking
            playerrating.innerText = Number(response.ProfileInfo.StatsGlobal.PlayerRating).toFixed(2);
            playtime.innerText = response.ProfileInfo.Playtime
            htmlcompletion.innerText = response.ProfileInfo.StatsGlobal.Completion
        }
        // Sinon si la value est égale à "4K"
        else if(value === "4K"){
            // Alors le texte de la page doit valoir ce qui est contenu dans response.ProfileInfo.Stats4K
            global_ranking_value.innerText ="#" + response.ProfileInfo.Stats4K.GlobalRanking
            country_ranking_value.innerText ="#" + response.ProfileInfo.Stats4K.CountryRanking
            playerrating.innerText = response.ProfileInfo.Stats4K.PlayerRating
            htmlcompletion.innerText = response.ProfileInfo.Stats4K.Completion
            //playtime.innerText = response.ProfileInfo.Stats4K.Playtime
        }
        // Sinon si la value est égale à "7K"
        else if(value === "7K"){
            // Alors le texte de la page doit valoir ce qui est contenu dans response.ProfileInfo.Stats4K
            global_ranking_value.innerText ="#" + response.ProfileInfo.Stats7K.GlobalRanking
            country_ranking_value.innerText ="#" + response.ProfileInfo.Stats7K.CountryRanking
            playerrating.innerText = response.ProfileInfo.Stats7K.PlayerRating
            htmlcompletion.innerText = response.ProfileInfo.Stats7K.Completion
            //playtime.innerText = response.ProfileInfo.Stats7K.Playtime
        }
        // Enfin, pour toutes les autres valeurs (qui ne sont pas encore implémentées)

    } // FIN DE LA FONCTION

    // on spécifie qu'à chaque fois que le keymodefilter change, on veut que la fonction changeValues s'exécute
    keymodefilter.onchange = changeValues

    keymodefilterTopPlays.onchange = () => showTopPlays(response, keymodefilterTopPlays.value);

    showTopPlays(response, "any");
}

function showTopPlays(response, keymode){
    // On vide la div pour mettre les nouveaux scores filtrés;
    topscoresBox.innerHTML = "";
    topscoresMainBox.style.height = "25rem";
    showMoreText.innerText = "show more \xa0v"

    var topPlays = response.ProfileInfo.TopPlays;

    switch(keymode){
        case "4":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 4);
            break;
        case "5":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 5);
            break;
        case "6":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 6);
            break;
        case "7":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 7);
            break;
        case "8":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 8);
            break;
        case "9":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 9);
            break;
        case "10":
            topPlays = response.ProfileInfo.TopPlays.filter(play => play.Keymode === 10);
            break;
        case "any":
            topPlays = response.ProfileInfo.TopPlays;
            break;
    }

    topscoresNumberofmaps.innerText = topPlays.length;

    const mapsLength = topPlays.length > 100 ? 99 : topPlays.length
    const height = (25 + mapsLength * 3.75 - 5 * 3.75) + "rem";
    const gap = (5 * mapsLength) + "px";
    const styleHeight = "calc(" + height + " + " + gap + ")";
    var i = 1;
    topPlays.forEach(play => {
        if(i < 100){
            let div = document.createElement("div");
            div.classList.add("scoretemplate");
            div.classList.add("topscore");

            let chartBg = document.createElement("img");
            chartBg.classList.add("chartbg");
            chartBg.src = play.ChartBackground;
            
            div.appendChild(chartBg);

            let topscoreGrade = document.createElement("img");
            topscoreGrade.classList.add("topscore-grade");
            topscoreGrade.src = "/assets/images/grades/" + play.Grade.toLowerCase().replace("+", "plus") + ".png";
            div.appendChild(topscoreGrade);

            let nameBox = document.createElement("div");
            nameBox.classList.add("topscore-namebox");

            let name = document.createElement("span");
            name.classList.add("topscore-name");
            let mapLink = document.createElement("a");
            mapLink.classList.add("chartpagelink");
            mapLink.href = "/charts/chartpage/" + play.ChartHash;
            mapLink.target = "_blank";
            mapLink.innerText = play.ChartName;
            name.appendChild(mapLink);

            nameBox.appendChild(name);

            let diffName = document.createElement("span");
            diffName.classList.add("topscore-diffname");
            let mapLinkDiff = document.createElement("a");
            mapLinkDiff.classList.add("chartpagelink");
            mapLinkDiff.href = "/charts/chartpage/" + play.ChartHash;
            mapLinkDiff.target = "_blank";
            mapLinkDiff.innerText = play.ChartDiffName;
            diffName.appendChild(mapLinkDiff);

            nameBox.appendChild(diffName);

            div.appendChild(nameBox);

            let topscoreEndBox = document.createElement("div");
            topscoreEndBox.classList.add("topscore-endbox");
            
            let topscoreRateBox = document.createElement("div");
            topscoreRateBox.classList.add("topscore-ratebox");
            let topscoreRate = document.createElement("span");
            topscoreRate.classList.add("topscore-rate");
            topscoreRate.innerText = Number(play.Rate).toFixed(2) + "x";

            topscoreRateBox.appendChild(topscoreRate);

            let topscoreAccBox = document.createElement("div");
            topscoreAccBox.classList.add("topscore-accbox");
            let topscoreAcc = document.createElement("span");
            topscoreAcc.classList.add("topscore-acc");
            if(play.Accuracy === 1){
                topscoreAcc.innerText = "100%";
            } else {
                topscoreAcc.innerText = Number(play.Accuracy * 100).toFixed(2) + "%";
            }

            topscoreAccBox.appendChild(topscoreAcc);

            let topscoreRatingBox = document.createElement("div");
            topscoreRatingBox.classList.add("topscore-ratingbox");
            let topscoreRatingvalue = document.createElement("span");
            topscoreRatingvalue.classList.add("topscore-ratingvalue");
            topscoreRatingvalue.innerText = Number(play.Rating).toFixed(2);

            topscoreRatingBox.appendChild(topscoreRatingvalue);

            topscoreEndBox.appendChild(topscoreRateBox);
            topscoreEndBox.appendChild(topscoreAccBox);
            topscoreEndBox.appendChild(topscoreRatingBox);

            div.appendChild(topscoreEndBox);

            if(i > 5){
                div.classList.add("invisible");
            }

            topscoresBox.append(div);

        }
        i++;
    });

    showMoreButton.onclick = (event) => {
        if(showMoreText.innerText.includes("show more")){
            topscoresMainBox.style.height = styleHeight;
            const hiddenPlays = document.getElementsByClassName("invisible");
            [...hiddenPlays].forEach(hiddenPlay => hiddenPlay.classList.remove("invisible"));
            showMoreText.innerText = "show less \xa0^"
        } else {
            showTopPlays(response, keymode);
        }
    }
}

function reloadImage(isAvatar, isBanner, isBackground){
    const timestamp = (new Date()).getTime()
    if(isAvatar){
        const baseSrcAvatar = avatar.src;
        avatar.src = baseSrcAvatar + "?timestamp=" + timestamp;
    }
    if(isBanner){
        const baseSrcBanner = banner.src;
        banner.src = baseSrcBanner + "?timestamp=" + timestamp;
    }
    if(isBackground){
        body.style.backgroundImage = "url(" + backgroundImage + "?timestamp=" + timestamp + ")";
    }
}

function submitImage(){
    if(avatarUploadButton.files.length !== 0){
        const file = avatarUploadButton.files[0];
        if(isImageValid(file.name)){
            fetch(getApiEndpoint() + "/web/user/picture?type=avatar", {
                method: "POST",
                credentials: "include",
                body: file
            })
            .then((response) => response.json())
            .then((json) => {
                if (json.Success){
                    reloadImage(true, false, false);
                    settingsBox.close();
                } else {
                    settingsBox.close();
                }
            })
            .catch((reason => settingsBox.close()));
        }
    }

    if(bannerUploadButton.files.length !== 0){
        const file = bannerUploadButton.files[0];
        if(isImageValid(file.name)){
            fetch(getApiEndpoint() + "/web/user/picture?type=banner", {
                method: "POST",
                credentials: "include",
                body: file
            })
            .then((response) => response.json())
            .then((json) => {
                if (json.Success){
                    reloadImage(false, true, false);
                    settingsBox.close();
                } else {
                    settingsBox.close();
                }
            })
            .catch((reason => settingsBox.close()));
        }
    }

    if(backgroundUploadButton.files.length !== 0){
        const file = backgroundUploadButton.files[0];
        if(isImageValid(file.name)){
            fetch(getApiEndpoint() + "/web/user/picture?type=background", {
                method: "POST",
                credentials: "include",
                body: file
            })
            .then((response) => response.json())
            .then((json) => {
                if (json.Success){
                    reloadImage(false, false, true);
                    settingsBox.close();
                } else {
                    settingsBox.close();
                }
            })
            .catch((reason => settingsBox.close()));
        }
    }
}

function setColorTheme(primary, secondary){
    document.documentElement.style.setProperty('--main-color', primary);
    document.documentElement.style.setProperty('--secondary-color', secondary);
}

function submitTheme(){
    const primaryColor = primaryColorPicker.value;
    const secondaryColor = secondaryColorPicker.value;
    const primaryAlpha = Math.round(alphaPrimaryInput.value * 255).toString(16);
    const secondaryAlpha = Math.round(alphaSecondaryInput.value * 255).toString(16);

    const finalPrimaryColor = primaryColorPicker.value + "" + primaryAlpha;
    const finalSecondaryColor = secondaryColorPicker.value + "" + secondaryAlpha;
    const body = {
        "PrimaryColor": finalPrimaryColor,
        "SecondaryColor": finalSecondaryColor
    };
    fetch(getApiEndpoint() + "/web/user/theme", {
        method: "POST",
        credentials: "include",
        body: JSON.stringify(body)
    })
    .then((response) => response.json())
    .then((json) => {
        if (json.Success){
            setColorTheme(finalPrimaryColor, finalSecondaryColor);
            settingsBox.close();
        } else {
            settingsBox.close();
        }
    })
    .catch((reason => settingsBox.close()));
}

window.onload = (event) => {
    const parts = location.pathname.split("/").filter(Boolean);
    let username = parts.at(-1);
    let page = parts.at(-2);

    if(username && page === "profile"){
        // On crée une requête vide que l'on met dans une variable nommée "request". En JS, ce qui représente une requête se nomme XMLHttpRequest
        var request=new XMLHttpRequest()

        // On remplit la requête vide grâce à ".open()". Dans le "open()", on remplit les informations de la requête,
        // à savoir : la méthode (GET) et l'url. Le "false" indique qu'on doit attendre que le serveur réponde avant de continuer le code.
        request.open("get", getApiEndpoint() + "/web/user?name=" + username, false)

        // On envoie la requête au serveur (l'équivalent invisible de coller l'url dans le navigateur)
        request.send()

        // Comme on a envoyé la requête au serveur (étape 3),
        // on peut récupérer ce que le serveur nous a renvoyé grâce à la propriété "responseText" !
        // le JSON.parse permet de convertir la réponse du serveur en code utilisable en JS
        var response=JSON.parse(request.responseText)
        init(response, false)
    } else {

        fetch(getApiEndpoint() + "/web/login/verify", {
            method: "GET",
            credentials: "include"
        })
        .then((response) => response.json())
        .then((json) => {
            if (json.Success){
                fetch(getApiEndpoint() + "/web/user", {
                    method: "GET",
                    credentials: "include"
                })
                .then((response) => response.json())
                .then((json) => init(json, true))
            } else {
            }
        });
    }
}
