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

export {getApiEndpoint, getDiffColor};