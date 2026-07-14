import { getApiEndpoint } from "./utils.js"

function init(response){



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
    htmlglobalranking.innerText=response.ProfileInfo.StatsGlobal.GlobalRanking

    var htmlcountryranking=document.getElementById("countryranking")
    htmlcountryranking.innerText=response.ProfileInfo.StatsGlobal.CountryRanking

    var htmlplayerrating=document.getElementById("playerrating")
    htmlplayerrating.innerText=response.ProfileInfo.StatsGlobal.PlayerRating

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
    var htmllevel=document.getElementById("avatarimg")
    .src=response.ProfileInfo.Avatar

    var htmllevel=document.getElementById("bannerimg")
        .src=response.ProfileInfo.Banner





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
        var value = keymodefilter.value
        console.log(value)
        // Si la value est égale à "global"
        if(value === "global"){
            // Alors le texte de la page doit valoir ce qui est contenu dans response.ProfileInfo.StatsGlobal
            global_ranking_value.innerText = response.ProfileInfo.StatsGlobal.GlobalRanking
            country_ranking_value.innerText = response.ProfileInfo.StatsGlobal.CountryRanking
            playerrating.innerText = response.ProfileInfo.StatsGlobal.PlayerRating
            playtime.innerText = response.ProfileInfo.Playtime
            htmlcompletion.innerText = response.ProfileInfo.StatsGlobal.Completion
        }
        // Sinon si la value est égale à "4K"
        else if(value === "4K"){
            // Alors le texte de la page doit valoir ce qui est contenu dans response.ProfileInfo.Stats4K
            global_ranking_value.innerText = response.ProfileInfo.Stats4K.GlobalRanking
            country_ranking_value.innerText = response.ProfileInfo.Stats4K.CountryRanking
            playerrating.innerText = response.ProfileInfo.Stats4K.PlayerRating
            htmlcompletion.innerText = response.ProfileInfo.Stats4K.Completion
            //playtime.innerText = response.ProfileInfo.Stats4K.Playtime
        }
        // Sinon si la value est égale à "7K"
        else if(value === "7K"){
            // Alors le texte de la page doit valoir ce qui est contenu dans response.ProfileInfo.Stats4K
            global_ranking_value.innerText = response.ProfileInfo.Stats7K.GlobalRanking
            country_ranking_value.innerText = response.ProfileInfo.Stats7K.CountryRanking
            playerrating.innerText = response.ProfileInfo.Stats7K.PlayerRating
            htmlcompletion.innerText = response.ProfileInfo.Stats7K.Completion
            //playtime.innerText = response.ProfileInfo.Stats7K.Playtime
        }
        // Enfin, pour toutes les autres valeurs (qui ne sont pas encore implémentées)

    } // FIN DE LA FONCTION

    // on spécifie qu'à chaque fois que le keymodefilter change, on veut que la fonction changeValues s'exécute
    keymodefilter.onchange = changeValues
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
        init(response)
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
                .then((json) => init(json))
            } else {
            }
        });
    }
}
