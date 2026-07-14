var cat=document.getElementById('cat');
var counter=0
var number=document.getElementById('number');

function catclicked(){
    counter++;
    number.innerText="clicks:"+counter;
}

cat.addEventListener("click", catclicked);

