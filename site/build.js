const fs=require('fs'),path=require('path');
const d=__dirname;
const f=(p)=>fs.readFileSync(path.join(d,'node_modules/@fontsource',p)).toString('base64');
const face=(fam,w,file)=>`@font-face{font-family:"${fam}";font-weight:${w};font-style:normal;font-display:block;src:url(data:font/woff2;base64,${f(file)}) format("woff2")}`;
const css=[
 ...[400,500,600,700].map(w=>face('Chakra Petch',w,`chakra-petch/files/chakra-petch-latin-${w}-normal.woff2`)),
 ...[400,500,600].map(w=>face('IBM Plex Mono',w,`ibm-plex-mono/files/ibm-plex-mono-latin-${w}-normal.woff2`)),
].join('\n');
let h=fs.readFileSync(path.join(d,'index.src.html'),'utf8').replace('/*FONTS*/',css);
fs.writeFileSync(path.join(d,'index.html'),h);
console.log('index.html',(h.length/1024).toFixed(0)+'KB');
