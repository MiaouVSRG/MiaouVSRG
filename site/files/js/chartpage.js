const parts = location.pathname.split("/").filter(Boolean);
let chartId = parts.at(-1);
let cachedChartId = chartId;

let data = null;
let leaderboard = null;


// constants
const DARK_COLOR_TARGET = 12.5;
const LIGHT_COLOR_TARGET = 92.5;
const COLOR_OFFSET = 10;
// const diffColors = [
//   '#4290FB',
//   '#4AB1FE',
//   '#4FD5F3',
//   '#4FFFE0',
//   '#64FF8E',
//   '#9FFF50',
//   '#E0F85A',
//   '#FDB96C',
//   '#FF7769',
//   '#FF4E6F',
//   '#DB4A9A',
//   '#A94DBF',
//   '#6962DA',
//   '#3133B4',
//   '#0F0D69',
//   '#000000'
// ];
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

const PROD_URL = "https://beta.api.miaouvsrg.com"
const DEV_URL = "https://api.miaou.dev.internal"

const ENV = "prod";
const URL = ENV === "dev" ? DEV_URL : PROD_URL

// All HTML elements that needs to be updated
const bg = document.getElementById("bg");
const chartDiffBox = document.getElementById("chartdiffbox");
const banner = document.getElementById("banner");
const keymode = document.getElementById("keymode");
const chartDiffname = document.getElementById("chart-diffname");
const downloadButton = document.getElementById("button-download");
const miaouDirectButton = document.getElementById("button-miaoudirect");
const chartStatus = document.getElementById("chart-status");
const chartDifficulty = document.getElementById("chart-difficulty");
const chartLength = document.getElementById("chart-length");
const chartBPM = document.getElementById("chart-bpm");
const chartRicecount = document.getElementById("chart-ricecount");
const chartNoodlecount = document.getElementById("chart-noodlecount");
const chartTitle = document.getElementById("chart-title");
const chartArtist = document.getElementById("chart-artist");
const chartMapper = document.getElementById("chart-mapper");
const audioPreview = document.getElementById("audio-preview");
const audioPlayButton = document.getElementById("audio-play-button");
const audioLength = document.getElementById("audio-length");
const audioLengthText = document.getElementById("audio-length-text");
const audioVolume = document.getElementById("audio-volume");
const audioButtons = document.getElementById("audio-buttons");

const leaderboardPlayerCardBox = document.getElementById("leaderboard-player-card-box");

function shortTimeAgo(timestamp) {
  const diff = Date.now() - timestamp;

  const units = [
    ["y", 365 * 24 * 60 * 60 * 1000],
    ["mo", 30 * 24 * 60 * 60 * 1000],
    ["d", 24 * 60 * 60 * 1000],
    ["h", 60 * 60 * 1000],
    ["m", 60 * 1000],
    ["s", 1000],
  ];

  for (const [unit, ms] of units) {
    const value = Math.floor(diff / ms);
    if (value >= 1) {
      return `${value}${unit}`;
    }
  }

  return "just now";
}

function createSpan(innerText, link){
    const span = document.createElement("span");
    span.classList.add("leaderboardplayerspan");

    if(link){
        const a = document.createElement("a");
        a.href = link;
        a.innerText = innerText;
        a.target = "_blank";
        span.appendChild(a);
    } else {
        span.innerText = innerText;
    }
    return span;
}

function createLeaderboard(){
    leaderboardPlayerCardBox.innerHTML = "";
    leaderboard.forEach(score => {
        const main = document.createElement("div");
        main.classList.add("leaderboardplayercard")

        const playercarddivrank = document.createElement("div");
        playercarddivrank.classList.add("playercarddivrank");
        const playercarddivrating = document.createElement("div");
        playercarddivrating.classList.add("playercarddivrating");
        const playercarddivacc = document.createElement("div");
        playercarddivacc.classList.add("playercarddivacc");
        const playercarddivplayer = document.createElement("div");
        playercarddivplayer.classList.add("playercarddivplayer");
        const playercarddivcombo = document.createElement("div");
        playercarddivcombo.classList.add("playercarddivcombo");
        const playercarddivperf = document.createElement("div");
        playercarddivperf.classList.add("playercarddivperf");
        const playercarddivgreat = document.createElement("div");
        playercarddivgreat.classList.add("playercarddivgreat");
        const playercarddivmeh = document.createElement("div");
        playercarddivmeh.classList.add("playercarddivmeh");
        const playercarddivmiss = document.createElement("div");
        playercarddivmiss.classList.add("playercarddivmiss");
        const playercarddivtime = document.createElement("div");
        playercarddivtime.classList.add("playercarddivtime");
        const playercarddivmods = document.createElement("div");
        playercarddivmods.classList.add("playercarddivmods");

        let span = createSpan("#" + score.Rank);
        playercarddivrank.appendChild(span);
        main.appendChild(playercarddivrank);


        span = createSpan(Number(score.Rating).toFixed(2));
        playercarddivrating.appendChild(span);
        main.appendChild(playercarddivrating);


        span = createSpan(Number(score.Acc * 100).toFixed(2) + "%");
        playercarddivacc.appendChild(span);
        main.appendChild(playercarddivacc);


        span = createSpan(score.Username, "/user/profile/" + score.Username);
        playercarddivplayer.appendChild(span);
        main.appendChild(playercarddivplayer);


        span = createSpan(score.Combo);
        playercarddivcombo.appendChild(span);
        main.appendChild(playercarddivcombo);


        span = createSpan(score.PerfectCount);
        playercarddivperf.appendChild(span);
        main.appendChild(playercarddivperf);


        span = createSpan(score.GreatCount);
        playercarddivgreat.appendChild(span);
        main.appendChild(playercarddivgreat);


        span = createSpan(score.MehCount);
        playercarddivmeh.appendChild(span);
        main.appendChild(playercarddivmeh);


        span = createSpan(score.MissCount);
        playercarddivmiss.appendChild(span);
        main.appendChild(playercarddivmiss);


        span = createSpan(shortTimeAgo(score.Timestamp));
        playercarddivtime.appendChild(span);
        main.appendChild(playercarddivtime);


        span = createSpan(Number(score.Rate).toFixed(2) + "x");
        playercarddivmods.appendChild(span);
        main.appendChild(playercarddivmods);


        leaderboardPlayerCardBox.appendChild(main);
    });
}

function createDiffImages(){
    const difficulties = data.Difficulties.sort((a,b) => a.Rating - b.Rating);

    const overflowing = parseInt(difficulties.length / 15) + 1 > 1;

    const width = overflowing ? 60 : 3.5 * difficulties.length;
    chartDiffBox.style.width = width + "rem";

    const height = 2.5 * (parseInt(difficulties.length / 15) + 1);

    let i = 0;

    difficulties.forEach(difficulty => {
        i = i + 1;

        if (i === 17){
            chartDiffBox.style.justifyContent = "start";
            const diffDiv = document.createElement("div");
            diffDiv.innerText = "...";

            diffDiv.addEventListener("click", () => {
                if(diffDiv.classList.contains("clicked")){
                    diffDiv.classList.remove("clicked");
                    chartDiffBox.style.height = 2.5 + "rem";
                    chartMapper.style.marginTop = 10.3 + "rem";
                    chartDiffname.style.marginTop = 28.5 + "rem";
                    let elements = document.getElementsByClassName("displayed");
                    [...elements].forEach((element) => {
                        element.classList.add("displaynone");
                        element.classList.remove("displayed");
                    });
                } else {
                    diffDiv.classList.add("clicked");
                    chartDiffBox.style.height = height + "rem";
                    chartMapper.style.marginTop = 10.3 + height - 2.5 + "rem";
                    chartDiffname.style.marginTop = 28.5 + height - 2.5 + "rem";
                    let elements = document.getElementsByClassName("displaynone");

                    [...elements].forEach((element) => {
                        element.classList.remove("displaynone");
                        element.classList.add("displayed");
                    });
                }
            });

            chartDiffBox.appendChild(diffDiv);
        }


        const diffDiv = document.createElement("div");
        diffDiv.style.width = "3.5rem";
        const image = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        image.classList.add("difficulty");
        image.classList.add("chartdifflogo");
        image.id = difficulty.Hash;
        image.setAttribute("viewBox", "0 0 200 200");
        image.style.width = 32 + "px";
        image.style.height = 32 + "px";

        let colorValue = difficulty.Rating > 10 ? 10 : Math.floor(difficulty.Rating);
        const color = diffColors[colorValue];
        image.style.color = color;
        
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        // const path2 = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute("fill", "currentColor");
        path.setAttribute("d", "M0 0 C4.03720213 4.14410623 6.53271953 8.55784877 9 13.75 C9.34538818 14.45809814 9.69077637 15.16619629 10.04663086 15.89575195 C10.7308139 17.29886358 11.41183589 18.70352196 12.08935547 20.10986328 C15.15121556 26.46506995 18.34786804 32.75331526 21.625 39 C22.19476563 40.093125 22.76453125 41.18625 23.3515625 42.3125 C24.87698379 45.22076822 24.87698379 45.22076822 28 47 C29.01351664 48.65848177 30.01120795 50.32665961 31 52 C34.46017819 56.46180872 37.90923426 60.50144692 43.0625 63 C45 64 45 64 46.75 65.875 C49.78278276 68.73929483 53.26814286 70.24455404 57 72 C58.83551326 72.91229379 60.66866157 73.82935432 62.5 74.75 C66.02691827 76.52243348 69.56145512 78.27458607 73.1171875 79.98828125 C73.82294922 80.33213867 74.52871094 80.67599609 75.25585938 81.03027344 C76.63153411 81.70036017 78.01029652 82.36415822 79.39257812 83.02050781 C89.70931755 88.0751917 89.70931755 88.0751917 91.91015625 93.0625 C91.96042969 93.701875 92.01070312 94.34125 92.0625 95 C92.13597656 95.639375 92.20945313 96.27875 92.28515625 96.9375 C91.86653297 99.96535056 90.86270438 101.56271141 89 104 C86.65565555 105.68508638 84.42872827 107.14074619 81.9375 108.5625 C80.92876831 109.15381714 80.92876831 109.15381714 79.8996582 109.75708008 C75.50445524 112.28312529 71.10064389 114.31108456 66.33203125 116.04296875 C62.76934664 117.505042 59.60993733 119.53852772 56.35546875 121.58203125 C54.32313191 122.80547781 52.35989474 123.75387123 50.1875 124.6875 C29.82849966 134.72304764 17.57470633 157.35049339 9.984375 177.72265625 C7.1396385 184.30393156 3.67152813 190.86530981 -3 194 C-5.0625 194.28515625 -5.0625 194.28515625 -7 194.0625 C-7.639375 194.01222656 -8.27875 193.96195313 -8.9375 193.91015625 C-14.71702864 191.35972032 -17.49446315 184.52868475 -20.0625 179.125 C-20.38798828 178.45597656 -20.71347656 177.78695312 -21.04882812 177.09765625 C-21.70709415 175.74462627 -22.36301551 174.3904531 -23.01660156 173.03515625 C-25.85991451 167.15088198 -28.83799811 161.39914393 -32.125 155.75 C-36.27676849 148.58858521 -36.27676849 148.58858521 -38 145 C-38.66 145 -39.32 145 -40 145 C-41.01375366 143.34166309 -42.01105005 141.67324707 -43 140 C-44.40823455 138.41243971 -45.86929127 136.87043085 -47.375 135.375 C-48.12523437 134.61960938 -48.87546875 133.86421875 -49.6484375 133.0859375 C-51.95292975 130.90824995 -51.95292975 130.90824995 -54.7890625 129.3515625 C-55.51867188 128.90554687 -56.24828125 128.45953125 -57 128 C-57 127.34 -57 126.68 -57 126 C-57.85335937 125.74863281 -58.70671875 125.49726563 -59.5859375 125.23828125 C-63.24788266 123.9100929 -66.28853031 122.19321684 -69.625 120.1875 C-74.9893434 117.02102752 -80.36872141 114.51671146 -86.16796875 112.21875 C-91.74413475 109.81907581 -98.08655553 106.78708804 -102 102 C-102.93637838 97.85318147 -103.45924002 95.31648806 -102.0625 91.3125 C-95.5444053 84.00433321 -84.68056307 80.25927826 -76 76 C-62.22311886 69.20568719 -47.42785526 61.30435013 -39 48 C-38.54882812 47.37351563 -38.09765625 46.74703125 -37.6328125 46.1015625 C-31.35538537 37.24530045 -26.56631951 27.2924076 -21.8203125 17.5546875 C-21.47145996 16.8419165 -21.12260742 16.12914551 -20.76318359 15.39477539 C-20.10958578 14.05151377 -19.46349267 12.70456206 -18.82666016 11.35327148 C-16.69013962 6.94936421 -14.33344592 3.54045929 -11 0 C-7.22869715 -1.25710095 -3.70253542 -1.45795809 0 0 Z ")
        // path.setAttribute("d", "M0 0 C1.26674561 0.00829834 2.53349121 0.01659668 3.83862305 0.02514648 C30.03623988 0.49592287 52.8401568 11.36847784 71.6875 29.4375 C89.77817509 48.29472912 99.34741744 72.68512799 99.125 98.75 C99.11670166 100.01674561 99.10840332 101.28349121 99.09985352 102.58862305 C98.62907713 128.78623988 87.75652216 151.5901568 69.6875 170.4375 C50.83027088 188.52817509 26.43987201 198.09741744 0.375 197.875 C-0.89174561 197.86670166 -2.15849121 197.85840332 -3.46362305 197.84985352 C-29.66123988 197.37907713 -52.4651568 186.50652216 -71.3125 168.4375 C-89.40317509 149.58027088 -98.97241744 125.18987201 -98.75 99.125 C-98.74170166 97.85825439 -98.73340332 96.59150879 -98.72485352 95.28637695 C-98.25407713 69.08876012 -87.38152216 46.2848432 -69.3125 27.4375 C-50.45527088 9.34682491 -26.06487201 -0.22241744 0 0 Z M-50.3125 39.4375 C-51.323125 40.200625 -52.33375 40.96375 -53.375 41.75 C-68.66780427 55.74128902 -76.96073924 75.31012821 -78.27734375 95.77734375 C-78.46928404 115.76046097 -72.23222552 134.08763564 -59.3125 149.4375 C-58.549375 150.448125 -57.78625 151.45875 -57 152.5 C-43.00871098 167.79280427 -23.43987179 176.08573924 -2.97265625 177.40234375 C17.01046097 177.59428404 35.33763564 171.35722552 50.6875 158.4375 C52.2034375 157.2928125 52.2034375 157.2928125 53.75 156.125 C69.04280427 142.13371098 77.33573924 122.56487179 78.65234375 102.09765625 C78.84428404 82.11453903 72.60722552 63.78736436 59.6875 48.4375 C58.924375 47.426875 58.16125 46.41625 57.375 45.375 C43.38371098 30.08219573 23.81487179 21.78926076 3.34765625 20.47265625 C-16.63546097 20.28071596 -34.96263564 26.51777448 -50.3125 39.4375 Z ");
        // path.setAttribute("transform", "translate(100.3125,0.5625)");
        path.setAttribute("transform", "translate(106,4)");

        // path2.setAttribute("fill", "currentColor")
        // path2.setAttribute("d", "M0 0 C0.7528125 -0.0309375 1.505625 -0.061875 2.28125 -0.09375 C5.59592695 0.79327622 6.51285322 2.73666723 8.5 5.5 C9.86118241 7.95880423 11.149525 10.40106182 12.390625 12.91796875 C12.74010193 13.6164241 13.08957886 14.31487946 13.449646 15.03450012 C14.18188156 16.50129935 14.91118749 17.96956438 15.63769531 19.43920898 C16.75344991 21.69447327 17.87990244 23.94415384 19.0078125 26.19335938 C19.71938636 27.62207907 20.43034587 29.05110495 21.140625 30.48046875 C21.47805725 31.15442184 21.8154895 31.82837494 22.16314697 32.52275085 C24.5 37.2689142 24.5 37.2689142 24.5 39.5 C25.27601562 39.55188477 26.05203125 39.60376953 26.8515625 39.65722656 C35.11224117 40.24621887 43.2968831 41.11601283 51.5 42.25 C52.68722656 42.394375 53.87445313 42.53875 55.09765625 42.6875 C59.96467393 43.36789481 63.61781675 43.94655485 67.8125 46.578125 C69.75637243 49.94390412 70.53177769 52.12396087 70.30126953 56.00244141 C68.50934929 61.58786008 63.63174416 65.30291201 59.4375 69.1875 C58.49455078 70.09822266 57.55160156 71.00894531 56.58007812 71.94726562 C52.61422642 75.75282507 48.79736813 79.30271955 44.25390625 82.39453125 C42.00057309 84.30292878 41.54446897 85.08445471 41.22583008 88.06201172 C41.44517698 91.26946765 41.92180775 94.33774755 42.5 97.5 C42.66532227 98.58619629 42.83064453 99.67239258 43.00097656 100.79150391 C43.68580429 105.27983052 44.44231902 109.75452305 45.21484375 114.22851562 C47.3759363 127.45160351 47.3759363 127.45160351 44 132.9375 C40.24992914 135.28129429 36.74428012 135.15717125 32.5 134.5 C28.4653681 132.95542517 24.76419665 130.92213189 21 128.8125 C20.01902344 128.28205078 19.03804688 127.75160156 18.02734375 127.20507812 C10.40065891 123.06676378 10.40065891 123.06676378 6.98828125 120.91943359 C6.21871094 120.4445752 5.44914062 119.9697168 4.65625 119.48046875 C3.99431641 119.05016357 3.33238281 118.6198584 2.65039062 118.17651367 C-1.46172423 116.88284085 -4.92782826 119.22628469 -8.62792969 120.99633789 C-9.71493164 121.56602295 -10.80193359 122.13570801 -11.921875 122.72265625 C-12.50793518 123.02819931 -13.09399536 123.33374237 -13.69781494 123.64854431 C-15.5507407 124.61572829 -17.40040451 125.5889652 -19.25 126.5625 C-20.50629583 127.21981134 -21.76280441 127.87671623 -23.01953125 128.53320312 C-25.3109198 129.73062966 -27.60075153 130.93085766 -29.88964844 132.13305664 C-34.19065134 134.38533079 -37.6168583 135.30296403 -42.5 134.5 C-45.5 132.5 -45.5 132.5 -47.5 129.5 C-48.17941372 121.67730586 -46.36766492 113.98702779 -45 106.3125 C-44.6403299 104.27831214 -44.28270539 102.24383664 -43.93005371 100.2084198 C-43.61111455 98.3696429 -43.28524796 96.53207151 -42.95898438 94.69458008 C-42.80751953 93.64036865 -42.65605469 92.58615723 -42.5 91.5 C-42.35111328 90.66525146 -42.20222656 89.83050293 -42.04882812 88.97045898 C-42.77985678 84.96760241 -45.73713857 82.83618072 -48.625 80.203125 C-49.90157114 78.9849296 -51.17759587 77.7661614 -52.453125 76.546875 C-54.47308002 74.64470161 -56.49950974 72.75045313 -58.53710938 70.8671875 C-60.50302659 69.04073769 -62.43966583 67.18687624 -64.375 65.328125 C-64.98746582 64.77779602 -65.59993164 64.22746704 -66.23095703 63.66046143 C-69.40498049 60.58166324 -70.92221927 58.37854381 -71.1953125 53.88574219 C-70.93399852 50.51664794 -70.46243458 49.44482678 -68.48217773 46.53637695 C-64.97035825 44.13833468 -61.91653101 43.41180065 -57.78515625 42.91796875 C-57.0517955 42.81406326 -56.31843475 42.71015778 -55.56285095 42.60310364 C-53.23160322 42.27818917 -50.89764018 41.9829805 -48.5625 41.6875 C-46.99779204 41.47123692 -45.43333431 41.25315509 -43.86914062 41.03320312 C-31.66049521 39.35915786 -31.66049521 39.35915786 -25.5 39.5 C-25.23880371 38.65526123 -24.97760742 37.81052246 -24.70849609 36.94018555 C-23.43283374 33.30880005 -21.86999124 29.89886025 -20.1640625 26.45703125 C-19.84343842 25.80675461 -19.52281433 25.15647797 -19.19247437 24.48649597 C-18.5171978 23.121517 -17.83972311 21.75762348 -17.16015625 20.39477539 C-16.12523039 18.31296343 -15.10548183 16.22414462 -14.0859375 14.13476562 C-13.42539916 12.80423038 -12.76397104 11.47413641 -12.1015625 10.14453125 C-11.64978134 9.21167046 -11.64978134 9.21167046 -11.18887329 8.25996399 C-8.55927058 3.05520178 -6.00866511 0.08231048 0 0 Z ")
        // path2.setAttribute("transform", "translate(101.5,27.5)")

        image.appendChild(path);
        // image.appendChild(path2);


        image.addEventListener("mouseover", () => {
            setDifficultyName(difficulty.Hash);
            if(!image.classList.contains("diff-selected")){
                image.classList.add("diff-focused");
                image.style.width = "22px";
                image.style.height = "22px";
                image.style.padding = "5px";
            }
        });

        image.addEventListener("mouseleave", () => {
            if(!image.classList.contains("diff-selected")){
                image.classList.remove("diff-focused");
                image.style.width = "32px";
                image.style.height = "32px";
                image.style.padding = "0";
            }
        });
        image.addEventListener("click", () => setChart(difficulty.Hash));

        if(i > 16){
            diffDiv.classList.add("displaynone");
        }

        diffDiv.appendChild(image);
        chartDiffBox.appendChild(diffDiv);
    });
}

function setChart(id){
    if(typeof id !== "string"){
        console.error("[ERROR] Tried to change chart " + id + " : The id is not a string.");
        return;
    }

    cachedChartId = id;

    const diff = data.Difficulties.filter((difficulty) => difficulty.Hash === id);
    if(diff.length !== 1){
        console.error("[ERROR] Tried to get chart with id " + id + " but " + diff.length + "chart(s) exist.");
        return;
    }

    const olderDiffSelected = document.getElementsByClassName("diff-selected");
    [...olderDiffSelected].forEach((element) => {
        element.classList.remove("diff-selected");
        element.style.width = "32px";
        element.style.height = "32px";
        element.style.padding = "0";
    });

    const chartElement = document.getElementById(id);
    chartElement.classList.remove("diff-focused");
    chartElement.classList.add("diff-selected");
    chartElement.style.width = "22px";
    chartElement.style.height = "22px";
    chartElement.style.padding = "5px";

    const difficulty = diff[0];
    keymode.innerText = difficulty.Keymode + "K";
    chartDiffname.innerText = difficulty.Name + " (" + Number((difficulty.Rating).toFixed(2)) + " ✪)";
    chartDifficulty.innerText = Number((difficulty.Rating).toFixed(2));
    chartLength.innerText = difficulty.Length.replace("m", ":").replace("s", "");
    chartBPM.innerText = difficulty.BPM;
    chartRicecount.innerText = difficulty.RiceCount;
    chartNoodlecount.innerText = difficulty.LNCount;
    chartArtist.innerText = difficulty.Artist;
    chartMapper.innerText = difficulty.Mapper;

    setLeaderboard(id);
}

function setDifficultyName(id){
    if(id === undefined){
        id = cachedChartId;
    }
    if(typeof id !== "string"){
        console.error("[ERROR] Tried to change chart " + id + " : The id is not a string.");
        return;
    }

    const diff = data.Difficulties.filter((difficulty) => difficulty.Hash === id);
    if(diff.length !== 1){
        console.error("[ERROR] Tried to get chart with id " + id + " but " + diff.length + "chart(s) exist.");
        return;
    }

    const difficulty = diff[0];
    chartDiffname.innerText = difficulty.Name + " (" + Number((difficulty.Rating).toFixed(2)) + " ✪)";
}

function formatTime(time){
    const minutes = Math.floor(time.toFixed(0) / 60);
    const seconds = time.toFixed(0) - minutes * 60 - 1;
    if(seconds === -1){
        return "0:00"
    }
    if(seconds < 10){
        return minutes + ":" + "0" + seconds;
    }
    return minutes + ":" + seconds;
}

function formatHslObject(hsl){
    return hsl.h + ", " + hsl.s + "%" + ", " + hsl.l + "%"
}

function setColorsByHsl(hsl1, hsl2){
    document.documentElement.style.setProperty('--main-color', formatHslObject(hsl1));
    document.documentElement.style.setProperty('--main-h', hsl1.h);
    document.documentElement.style.setProperty('--main-s', hsl1.s + "%");
    document.documentElement.style.setProperty('--main-l', hsl1.l + "%");
    document.documentElement.style.setProperty('--main-hsl', formatHslObject(hsl1));
    document.documentElement.style.setProperty('--dark-main-hsl', formatHslObject({h: hsl1.h, s: hsl1.s, l: hsl1.l - COLOR_OFFSET}));
    document.documentElement.style.setProperty('--light-main-hsl', formatHslObject({h: hsl1.h, s: hsl1.s, l: hsl1.l + COLOR_OFFSET}));

    if(hsl1.l + COLOR_OFFSET < 70){
        document.documentElement.style.setProperty('--secondary-text-hsl', formatHslObject({h: hsl1.h, s: hsl1.s, l: LIGHT_COLOR_TARGET}))
    } else {
        document.documentElement.style.setProperty('--secondary-text-hsl', formatHslObject({h: hsl1.h, s: hsl1.s, l: DARK_COLOR_TARGET}))
    }

    document.documentElement.style.setProperty('--secondary-color', formatHslObject(hsl2));
    document.documentElement.style.setProperty('--secondary-h', hsl2.h);
    document.documentElement.style.setProperty('--secondary-s', hsl2.s + "%");
    document.documentElement.style.setProperty('--secondary-l', hsl2.l + "%");
    document.documentElement.style.setProperty('--secondary-hsl', formatHslObject(hsl2));
}

async function setLeaderboard(id){
    const leaderboardReq = await fetch(URL + "/web/map/leaderboard?chart=" + id);
    if(leaderboardReq.status !== 200){
        return;
    }
    leaderboard = (await leaderboardReq.json()).Scores;
    
    createLeaderboard();
}

async function init(){
    const response = await fetch(URL + "/web/map?chart=" + chartId);
    if(response.status !== 200){
        return;
    }
    
    data = await response.json();
    
    downloadButton.addEventListener("click", () => document.location.href = data.DownloadLink);
    miaouDirectButton.addEventListener("click", () => document.location.href = data.MiaoudirectLink);

    banner.src = data.Background;
    bg.src = data.Background;
    audioPreview.src = data.Audio;
    chartTitle.innerText = data.Name;
    chartStatus.innerText = data.Ranked ? "RANKED" : "UNRANKED";

    chartDiffBox.addEventListener("mouseleave", () => setDifficultyName());
    audioPlayButton.addEventListener("click", () => {
        if(audioPreview.classList.contains("pause")){
            audioPreview.play();
            audioPlayButton.src = "/assets/images/mediapause.png"
            audioPreview.classList.remove("pause");
            audioPreview.classList.add("play");
            audioButtons.classList.remove("hidden");
            audioButtons.classList.add("visible");
        } else if (audioPreview.classList.contains("play")){
            audioPreview.pause();
            audioPlayButton.src = "/assets/images/mediaplay.png"
            audioPreview.classList.remove("play");
            audioPreview.classList.add("pause");
        }
    });

    audioPreview.ontimeupdate = (event) => {
        audioLengthText.innerText = formatTime(audioPreview.currentTime) + "/" + formatTime(audioPreview.duration);
        audioLength.value = audioPreview.currentTime / audioPreview.duration * 100
    }

    audioPreview.onended = (event) => {
        audioPlayButton.src = "/assets/images/mediaplay.png";
        audioPreview.classList.remove("play");
        audioPreview.classList.add("pause");
    }

    audioLength.onchange = (event) => {
        audioPreview.currentTime = audioPreview.duration * (audioLength.value / 100);
    }

    audioVolume.onchange = (event) => {
        audioPreview.volume = audioVolume.value / 10;
    }


    createDiffImages();
    setChart(chartId);

    bg.onload = async () => {
        const swatch = await ColorThief.getSwatches(bg);
        if(swatch.LightVibrant === null || swatch.LightMuted === null){
            const color = ColorThief.getPaletteSync(bg);
            setColorsByHsl(color[0].hsl(), color[1].hsl());
        } else {
            setColorsByHsl(swatch.LightMuted.color.hsl(), swatch.LightVibrant.color.hsl());
        }
    }

    setLeaderboard(chartId);
}

init();