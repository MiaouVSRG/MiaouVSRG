const ENV = "beta"

function getApiEndpoint(){
    switch(ENV){
        case "prod":
            return "https://api.miaouvsrg.com";
        case "beta":
            return "https://api.beta.miaouvsrg.com";
        case "local":
            return "https://api.miaou.dev.internal"
    }
}

async function checkCookie(){
    return fetch(getApiEndpoint() + "/web/login/verify", {
        method: "GET",
        credentials: "include"
    })
    .then((response) => response.json())
    .then((json) => {return json.Success})
    // Error means there is no token
    .catch((reason) => {return false});
}

/**
 * Returns true if the user stored in the local storage
 * 
 * OR
 * 
 * if the user has a valid cookie set
 * 
 * else, returns false.
 */
async function checkUserCache(){
    // Checks if cookie is still valid
    const isConnected = await checkCookie();
    if(!isConnected){
        return false;
    }

    if(localStorage.getItem('userInfo')){
        return true;
    } else {
        return fetch(getApiEndpoint() + "/web/user", {
            method: "GET",
            credentials: "include"
        })
        .then((response) => response.json())
        .then((json) => {
            localStorage.setItem('userInfo', JSON.stringify(json));
            return true;
        })
    };
}

async function getUserInfo(){
    let result = await checkUserCache()
    return result ? JSON.parse(localStorage.getItem('userInfo')) : null;
}

function setUserInfo(userInfo){
    localStorage.setItem('userInfo', JSON.stringify(userInfo));
}

function getDiffColor(difficulty){
    const diffColors = [
        '#4290FB',
        '#4FC0FF',
        '#4FFFD5',
        '#7CFF4F',
        '#F6F05C',
        '#FF8068',
        '#FF4E6F',
        '#C645B8',
        '#6563DE',
        '#18158E',
        '#000000'
    ];
    return diffColors[difficulty]
}

export {getApiEndpoint, getDiffColor, getUserInfo};