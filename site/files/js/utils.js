const ENV = "local"

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

export {getApiEndpoint};