/* Local concept underpainting, area sampling and integer-pixel cleanup.
 * No package, network, paid service, image generation API or game code. */
const fs=require('fs'),path=require('path');
const {Raster,decode,P,rgba}=require('./raster.cjs');
const OUT=__dirname,ROUND=Number(process.argv[2]||4);
const source=decode(path.join(OUT,'../boss_concept_4.png'));
const colors=Object.entries(P).map(([k,v])=>[k,rgba(v)]);
function nearest(rgb,allowed){
  // Preserve the source's burgundy material ramp. Pure RGB distance otherwise
  // collapses most dark shell/cloth planes into blue shadows and loses volume.
  if(ROUND>=3&&allowed?.includes('red')&&!allowed.includes('coolant')&&rgb[0]>=52&&rgb[0]-rgb[2]>=18&&rgb[0]-rgb[1]>=25)return rgb[0]>110&&rgb[1]>40?'warm':'red';
  let best=1e20,key='ink';for(const [k,c]of colors){if(allowed&&!allowed.includes(k))continue;const d=(c[0]-rgb[0])**2*.9+(c[1]-rgb[1])**2*1.2+(c[2]-rgb[2])**2;if(d<best){best=d;key=k;}}return key;
}
function clean(a){
  const old=Buffer.from(a.d),seen=new Set(),at=(x,y)=>(y*a.w+x)*4,key=i=>old.subarray(i,i+4).toString('hex');
  for(let y=0;y<a.h;y++)for(let x=0;x<a.w;x++){
    const i=at(x,y);if(seen.has(i)||!old[i+3])continue;
    const target=key(i),stack=[[x,y]],comp=[],near={};seen.add(i);
    while(stack.length){const [u,v]=stack.pop();comp.push([u,v]);for(const [dx,dy]of[[1,0],[-1,0],[0,1],[0,-1]]){
      const xx=u+dx,yy=v+dy;if(xx<0||yy<0||xx>=a.w||yy>=a.h)continue;const j=at(xx,yy);if(!old[j+3])continue;const k=key(j);
      if(k===target){if(!seen.has(j)){seen.add(j);stack.push([xx,yy]);}}else near[k]=(near[k]||0)+1;
    }}
    if(comp.length<=2){const fill=Object.entries(near).sort((a,b)=>b[1]-a[1])[0];if(fill)for(const [u,v]of comp)a.px(u,v,[...Buffer.from(fill[0],'hex')]);}
  }return a;
}
function extract(rect,w,h,points,allowed){
  const [sx,sy,sw,sh]=rect,mask=new Raster(source.w,source.h),a=new Raster(w,h);mask.poly(points,'white');
  for(let y=0;y<h;y++)for(let x=0;x<w;x++){
    let rgb=[0,0,0],n=0;for(let j=0;j<4;j++)for(let i=0;i<4;i++){
      const u=Math.min(source.w-1,Math.floor(sx+(x+(i+.5)/4)*sw/w)),v=Math.min(source.h-1,Math.floor(sy+(y+(j+.5)/4)*sh/h)),p=(v*source.w+u)*4;
      if(!mask.d[p+3])continue;n++;for(let k=0;k<3;k++)rgb[k]+=source.d[p+k];
    }if(n>=8)a.px(x,y,nearest(rgb.map(v=>v/n),allowed));
  }
  clean(a);const old=Buffer.from(a.d);
  for(let y=0;y<h;y++)for(let x=0;x<w;x++){if(!old[(y*w+x)*4+3])continue;for(const [dx,dy]of[[1,0],[-1,0],[0,1],[0,-1]]){const u=x+dx,v=y+dy;if(u<0||v<0||u>=w||v>=h||!old[(v*w+u)*4+3]){a.px(x,y,'ink');break;}}}
  return a;
}
function tube(a,p,w,c='dark'){a.line(p,'ink',w+2);a.line(p,c,w);}
function body(){
  const outline=[[29,256],[44,225],[65,202],[69,90],[63,85],[80,58],[82,43],[93,48],[171,42],[178,26],[235,25],[247,37],[260,99],[282,139],[290,93],[481,93],[499,120],[511,39],[520,14],[536,13],[550,28],[584,33],[677,45],[691,38],[694,50],[712,77],[695,86],[700,214],[726,242],[721,277],[683,298],[631,303],[583,291],[549,318],[528,362],[535,407],[543,480],[222,480],[235,420],[247,374],[228,335],[211,306],[177,294],[127,301],[87,286],[43,279]];
  const a=extract([20,0,720,482],384,224,outline,['ink','night','dark','mid','tile','coolant','red']);
  if(ROUND>=2)for(let y=95;y<224;y++)for(let x=0;x<384;x++)if(a.d[(y*384+x)*4+3])a.px(x,y,'ink');
  const map=pts=>pts.map(([x,y])=>[Math.round((x-20)*384/720),Math.round(y*224/482)]);
  // Erase source arms, face/halo and projectiles before rebuilding the torso.
  a.poly(map([[40,260],[69,201],[118,182],[153,139],[202,115],[271,131],[284,197],[252,274],[230,333],[204,347],[163,309],[107,304],[61,286]]),'ink');
  a.poly(map([[493,122],[530,115],[594,134],[653,190],[666,259],[706,289],[712,397],[673,456],[599,457],[580,340],[537,297],[518,228]]),'ink');
  a.poly(map([[269,75],[499,75],[499,224],[446,253],[320,253],[269,222]]),'ink');
  a.poly(map([[271,247],[486,246],[518,382],[465,420],[302,421],[261,367]]),'ink');
  a.poly([[116,91],[141,102],[162,114],[223,114],[249,94],[270,85],[280,128],[269,160],[258,186],[270,224],[111,224],[120,187],[111,157],[103,127]],'ink');
  a.poly([[132,117],[145,138],[149,169],[138,192],[130,224],[113,224],[121,189],[112,159],[112,133]],'night');
  a.poly([[245,112],[255,126],[257,158],[247,189],[253,224],[270,224],[259,187],[270,158],[273,129]],'night');
  for(const p of [ [[60,0],[62,25],[69,42],[75,67],[72,85],[66,101]],[[93,0],[98,22],[104,49],[113,72],[132,90],[147,107]],[[131,0],[129,26],[136,53],[133,76],[144,98]],[[257,0],[255,31],[248,61],[238,86],[228,100]],[[291,0],[285,34],[278,52],[258,79],[241,99]],[[318,0],[316,25],[308,45]] ])tube(a,p,3,'dark');
  for(const p of [[[18,76],[33,91],[53,97],[64,107]],[[0,114],[26,121],[53,116]],[[339,92],[360,94],[382,81]],[[325,113],[350,121],[382,114]]])tube(a,p,2,'dark');
  for(const [x,y]of[[99,42],[105,53],[72,76],[271,39],[289,56],[333,97]]){a.rect(x-2,y-1,5,3,'ink');a.rect(x,y,2,1,'red');}
  a.poly([[151,33],[157,29],[227,29],[232,35],[232,81],[225,87],[159,87],[150,81]],'ink');
  a.line([[153,34],[157,32],[226,32]],'tile',2);a.line([[153,34],[153,79],[158,83]],'coolant',1);
  a.line([[229,36],[229,80],[225,83],[160,83]],'dark',2);a.rect(156,36,72,44,'ink');
  a.rect(149,44,3,7,'dark');a.rect(231,67,3,7,'dark');
  a.poly([[159,101],[178,107],[204,107],[226,101],[240,113],[245,132],[241,153],[229,173],[209,183],[176,183],[155,172],[141,151],[139,129],[145,111]],'night');
  for(let i=0;i<5;i++){
    const y=104+i*14,o=[145,141,142,147,155][i],p=[[178,y+4],[165,y+1],[154,y-3],[o,y+1],[o-2,y+8],[o+2,y+15],[155,y+19],[173,y+22]],q=p.map(([x,y])=>[384-x,y+1]);
    tube(a,p,4,'mid');tube(a,q,4,'dark');a.line(p.slice(0,5).map(([x,y])=>[x,y-1]),'tile',1);a.line(q.slice(1,5).map(([x,y])=>[x-1,y-1]),'mid',1);
  }
  for(const p of [[[161,111],[155,115],[153,127],[158,132]],[[164,166],[164,181],[170,188],[176,186],[177,178]],[[223,164],[228,177],[226,188]],[[232,119],[237,132],[234,145]]])tube(a,p,1,'red');
  a.rect(157,112,70,54,'ink');a.line([[159,113],[225,113],[225,165]],'dark',2);a.rect(160,115,64,48,null);
  for(const x of [155,226])for(const y of [121,139,157]){a.rect(x,y,3,5,'dark');a.rect(x,y,1,3,'tile');}
  if(ROUND>=2){
    a.poly([[72,77],[88,70],[112,79],[134,96],[141,111],[131,124],[112,112],[93,99],[76,99]],'night');
    tube(a,[[91,81],[111,89],[124,101],[132,116]],4,'dark');tube(a,[[276,79],[259,86],[245,98],[237,109]],4,'dark');
    a.line([[99,84],[113,92],[120,100]],'mid',1);a.line([[256,88],[248,94]],'mid',1);
    for(const p of [[[127,167],[129,185],[121,204],[124,223]],[[247,167],[242,185],[252,208],[250,223]],[[180,183],[177,204],[180,223]],[[209,183],[215,202],[214,223]]])tube(a,p,2,'dark');
  }
  if(ROUND>=3){
    for(const p of [[[151,167],[158,177],[169,180]],[[233,167],[227,178],[216,181]]]){tube(a,p,3,'mid');a.line(p.map(([x,y])=>[x-1,y-1]),'tile',1);}
    a.poly([[129,177],[135,184],[134,197],[139,203],[133,213],[126,203]],'ink');a.line([[131,185],[131,194],[134,198]],'coolant',1);
    a.line([[144,195],[149,202],[153,220]],'night',3);a.line([[236,194],[230,207],[231,223]],'night',4);
    // A broad, continuous shadow belly makes this one creature, not loose props.
    a.poly([[151,188],[170,196],[208,196],[230,189],[240,224],[141,224]],'ink');
    a.line([[150,190],[157,204],[155,223]],'night',3);a.line([[229,191],[224,208],[227,223]],'night',3);
    for(const p of [[[122,121],[119,146],[127,167]],[[260,121],[264,146],[253,169]]])tube(a,p,2,'dark');
  }
  if(ROUND>=4){
    // Top-left rack glints remain subordinate to the empty display and arms.
    for(const [x,y,w]of[[47,40,22],[49,54,19],[49,69,18],[88,21,15],[91,34,13],[326,43,20],[329,58,18]]){a.rect(x,y,w,1,'mid');a.rect(x,y+2,Math.floor(w*.6),1,'dark');}
    for(const [x,y]of[[99,42],[271,39]]){a.rect(x-1,y,3,1,'red');a.rect(x,y-1,1,3,'red');}
    // Dark loose conduits emerging below the lowest ribs, with black oil seams.
    for(const p of [[[173,179],[169,193],[174,207],[172,223]],[[215,180],[221,195],[219,214],[224,223]]])tube(a,p,1,'dark');
    a.poly([[240,181],[244,184],[243,194],[246,200],[242,203]],'ink');
  }
  return a.scale(2);
}
function bug(lit=false){
  const outline=[[210,114],[218,113],[216,129],[208,140],[234,145],[260,155],[251,175],[240,188],[243,207],[231,228],[218,246],[224,263],[219,291],[207,305],[198,300],[199,279],[192,267],[179,281],[178,308],[166,327],[174,347],[163,367],[145,380],[103,380],[75,365],[59,343],[44,326],[43,306],[50,288],[47,270],[54,249],[70,229],[83,212],[100,208],[116,191],[139,176],[150,156],[164,151],[179,137],[195,136],[203,122]];
  const a=new Raster(100,160);a.blit(extract([40,112,224,270],100,122,outline,ROUND>=2?['ink','night','dark','red','warm']:['ink','night','dark','mid','red','warm']),0,0);
  if(ROUND>=2){a.poly([[0,36],[31,34],[38,46],[28,57],[19,65],[0,62]],null);}
  if(ROUND>=4)a.poly([[0,50],[25,52],[22,65],[14,74],[15,90],[0,95]],null);
  const legs=[[[65,42],[74,46],[76,59],[71,69]],[[57,49],[62,59],[61,71],[54,80]],[[48,57],[51,71],[43,82]],[[38,71],[36,85],[29,91]],[[28,82],[21,75],[19,65]],[[20,92],[12,87],[8,78]],[[50,91],[58,100],[56,113]]];
  for(const p of legs){tube(a,p,3,'red');a.line(p.map(([x,y])=>[x-1,y-1]),'warm',1);}
  a.poly([[25,119],[38,111],[54,113],[65,124],[73,143],[65,155],[55,159],[45,159],[32,153],[23,141]],'ink');
  a.poly([[28,121],[39,115],[52,117],[62,126],[68,142],[61,153],[53,159],[46,159],[34,149],[27,139]],'red');
  a.poly([[52,120],[62,129],[67,142],[60,153],[53,158],[47,157],[52,145],[50,132]],'dark');
  const claws=[[[31,127],[20,123],[15,110],[20,97],[29,94],[24,103],[25,112],[38,116]],[[28,141],[14,137],[7,127],[5,115],[10,108],[12,120],[19,125],[33,129]],[[62,123],[73,112],[82,114],[93,125],[94,140],[88,146],[88,133],[80,126],[68,134]],[[64,142],[77,140],[82,148],[80,157],[74,153],[70,150],[61,152]]];
  for(const p of claws){a.poly(p,'red','ink');a.line(p.slice(0,4).map(([x,y])=>[x,y-1]),'warm',1);}
  a.poly([[38,125],[48,121],[57,128],[60,141],[54,151],[43,152],[34,143],[33,133]],'ink');a.line([[36,131],[40,126],[47,124],[54,129]],'warm',2);
  a.poly([[40,132],[49,128],[55,134],[54,145],[45,149],[38,143]],lit?'tell':'night');
  for(const p of [[[39,133],[43,137]],[[50,130],[48,137]],[[55,140],[49,141]],[[45,148],[45,143]]])a.line(p,lit?'warm':'mid',2);
  if(lit){a.line([[41,133],[44,132],[47,133]],'white',1);a.rect(44,136,4,6,'tell');}
  if(ROUND>=2){for(const [x,y]of[[74,25],[40,70],[29,101]]){a.line([[x-8,y+1],[x-4,y-6],[x+3,y-8]],'warm',2);a.poly([[x+4,y-3],[x+8,y],[x+7,y+6],[x+2,y+8],[x,y+4]],'ink');a.line([[x-7,y+11],[x-1,y+15],[x+5,y+12]],'dark',2);}a.line([[48,101],[43,107],[45,116]],'ink',3);a.line([[44,108],[45,113]],'coolant',1);}
  if(ROUND>=3){a.poly([[23,107],[35,104],[43,110],[44,119],[38,126],[30,124],[25,116]],'red','ink');a.line([[26,110],[33,107],[38,111]],'warm',2);a.line([[33,114],[36,121],[42,121]],'dark',3);a.line([[18,127],[22,131],[27,132]],'dark',2);a.line([[76,119],[81,120],[85,125]],'warm',2);}
  if(ROUND>=2){
    // Reuse the concept's hooked claw silhouette, inverted to cup the low palm.
    // Fingers curl upward; the palm/base stays exactly on the bottom anchor.
    a.rect(0,ROUND>=3?104:116,100,ROUND>=3?56:44,null);
    const hand=extract([60,343,139,130],94,64,[[95,343],[128,346],[140,357],[154,359],[173,372],[188,385],[196,405],[194,426],[186,433],[180,434],[180,418],[178,407],[168,398],[153,397],[141,408],[125,414],[134,427],[149,439],[157,442],[153,447],[137,446],[118,435],[105,422],[99,415],[98,429],[110,441],[117,452],[111,457],[99,452],[93,448],[95,458],[108,462],[122,467],[115,472],[97,469],[83,462],[80,448],[69,441],[64,425],[64,410],[69,391],[66,385],[72,371],[83,364],[85,351]],['ink','night','dark','red','warm']);
    for(let y=0;y<64;y++)for(let x=0;x<94;x++){const i=((63-y)*94+x)*4;if(hand.d[i+3])a.px(x+3,y+96,[...hand.d.subarray(i,i+4)]);}
    a.poly([[34,140],[46,136],[59,140],[63,150],[59,159],[40,159],[32,151]],'red','ink');
    a.poly([[39,143],[48,140],[56,143],[59,150],[54,156],[42,156],[36,150]],lit?'tell':'ink');
    a.line([[39,144],[43,141],[49,141]],lit?'white':'warm',1);
    a.line([[39,147],[44,149]],lit?'warm':'mid',2);a.line([[53,144],[50,149]],lit?'warm':'mid',2);a.line([[52,154],[48,151]],lit?'warm':'mid',2);
    if(lit)a.rect(44,145,5,7,'tell');
    if(ROUND>=3){
      tube(a,[[28,101],[24,111],[27,123],[36,135]],5,'red');
      a.line([[26,106],[24,113],[28,121]],'warm',1);
      a.line([[70,128],[78,124],[84,120]],'warm',1);
    }
  }
  a.rect(49,158,3,2,'red');return a.scale(2);
}
function blade(lit=false){
  const a=new Raster(144,288);
  a.blit(extract([514,125,180,174],118,88,[[516,140],[525,128],[540,144],[557,146],[588,144],[612,157],[627,183],[636,207],[650,209],[654,218],[644,229],[653,244],[668,256],[680,280],[664,296],[623,286],[592,264],[570,254],[560,232],[536,218],[525,180]],['ink','night','dark','red','warm','brass','steel']),4,0);
  a.blit(extract([591,277,125,137],92,95,[[600,285],[619,294],[639,292],[650,280],[668,278],[685,290],[697,314],[707,339],[713,365],[705,388],[687,412],[660,402],[646,379],[618,365],[614,338],[597,320]],['ink','night','dark','red','warm','brass','steel']),44,77);
  a.blit(extract([595,367,108,111],72,64,[[602,391],[612,381],[635,378],[649,369],[668,373],[685,390],[693,415],[682,441],[672,457],[678,468],[667,477],[654,471],[650,458],[625,455],[609,444],[598,425],[597,409]],['ink','night','dark','red','warm','brass','steel']),36,126);
  for(const p of [[[12,9],[21,40],[36,52],[57,58],[81,54]],[[64,84],[58,99],[62,117],[75,128]],[[92,79],[105,78],[116,91],[122,112],[125,131]],[[76,133],[87,139],[94,153],[95,170]]]){a.line(p,'ink',4);a.line(p,'brass',2);}
  for(const p of [[[24,19],[31,30],[34,41]],[[40,17],[47,29],[53,43]],[[57,22],[65,35],[68,45]],[[85,94],[94,104],[96,117]],[[101,92],[111,103],[114,115]]])a.line(p,'ink',1);
  for(const [x,y]of[[23,19],[38,18],[54,21],[33,35],[48,36],[65,39],[85,94],[104,94],[91,113],[110,113]]){a.rect(x,y,3,3,'ink');a.rect(x,y,2,2,'brass');a.px(x,y,'steel');}
  a.poly([[113,106],[119,113],[117,124],[121,132],[116,140],[111,130]],'ink');a.line([[115,115],[114,122],[117,129]],'coolant',2);a.px(115,118,'cyan');
  a.rect(67,143,10,40,'ink');a.rect(69,145,6,36,'brass');for(let y=147;y<180;y+=5)a.line([[69,y],[74,y+2]],'dark',2);
  for(let i=0;i<4;i++){const x=48+i*9,y=155+i*2;a.poly([[x,y],[x+5,y-3],[x+11,y],[x+12,y+11],[x+7,y+17],[x,y+12]],'ink');a.poly([[x+2,y+1],[x+5,y],[x+8,y+2],[x+9,y+10],[x+5,y+13],[x+2,y+10]],'red');a.rect(x+2,y+1,5,3,'brass');a.rect(x+2,y,3,1,'steel');}
  a.poly([[79,146],[86,145],[93,151],[92,160],[86,166],[79,160],[75,154]],'ink');a.poly([[81,149],[86,148],[90,153],[89,159],[85,162],[80,157]],'red');a.line([[81,149],[86,148],[89,152]],'brass',2);
  a.poly([[52,179],[57,176],[85,176],[91,179],[89,184],[54,184]],'ink');a.poly([[54,179],[58,178],[84,178],[88,180],[86,182],[55,182]],'brass');a.line([[56,179],[84,179]],'steel',1);
  a.rect(63,183,19,5,'ink');a.rect(65,183,15,4,'brass');
  a.poly([[62,187],[83,187],[83,270],[72,287],[62,276]],'ink');a.poly([[64,188],[81,188],[81,270],[72,285],[64,275]],'steel');
  a.poly([[64,188],[68,188],[68,273],[72,285],[64,275]],lit?'cyan':'edge');a.poly([[77,189],[81,189],[81,270],[72,285],[77,268]],'mid');a.line([[75,190],[75,265]],'tile',1);
  if(lit){a.line([[64,189],[64,275],[72,285]],'white',1);a.line([[67,189],[67,273]],'cyan',1);}
  if(ROUND>=2){
    a.poly([[37,7],[55,9],[66,20],[61,25],[48,17],[40,19]],'red');a.line([[34,8],[42,10],[46,14]],'warm',2);
    a.poly([[53,61],[68,63],[80,73],[99,70],[106,81],[99,94],[83,99],[66,88],[58,74]],'ink');
    a.poly([[56,64],[67,66],[79,77],[98,74],[101,81],[95,90],[84,94],[69,85],[62,73]],'red');
    a.poly([[87,77],[98,74],[101,81],[95,90],[84,94],[81,87]],'dark');
    a.line([[57,66],[68,70],[76,80]],'warm',2);a.line([[64,57],[70,65],[84,68],[96,62]],'brass',2);
    a.line([[64,63],[73,71],[87,73],[99,67]],'ink',3);a.line([[66,64],[74,70],[88,71]],'mid',1);
    for(const [x,y]of[[69,74],[82,83],[94,80]]){a.rect(x,y,3,3,'ink');a.rect(x,y,2,1,'brass');}
  }
  if(ROUND>=3){a.poly([[52,28],[55,31],[54,37],[58,42],[55,45],[50,39]],'ink');a.line([[70,95],[73,106],[78,113]],'warm',2);for(const [x,y]of[[32,48],[96,81],[118,117]]){a.rect(x,y,3,2,'ink');a.px(x-1,y-1,'brass');}a.line([[67,202],[67,211]],lit?'white':'silver',1);a.line([[78,244],[80,242]],'ink',1);}
  if(ROUND>=4){
    // Remove the source's last crimson drip beside the new blade collar.
    for(let y=183;y<195;y++)for(let x=0;x<144;x++){const i=(y*144+x)*4,k='#'+a.d.subarray(i,i+3).toString('hex');if(a.d[i+3]&&(k===P.red||k===P.warm))a.px(x,y,'ink');}
  }
  a.px(72,287,'ink');return a.scale(2);
}
function hatch(){const a=new Raster(32,48);a.poly([[11,0],[31,0],[31,47],[10,47],[3,40],[0,26],[1,12],[5,4]],'ink');a.poly([[10,2],[29,2],[29,45],[12,45],[5,39],[2,25],[3,13],[7,6]],'night');
  for(let i=0;i<4;i++){const y=3+i*12,p=[[30,y+1],[23,y+4],[13,y+4],[6,y],[3,y+4],[5,y+9],[13,y+12],[24,y+12],[30,y+9]];tube(a,p,3,'mid');a.line(p.slice(0,5).map(([x,y])=>[x,y-1]),'tile',1);if(ROUND>=3)a.line([[7,y+4],[12,y+7],[23,y+8]],'dark',1);}
  a.rect(29,1,3,46,'ink');a.rect(29,2,1,43,'tile');for(const y of [5,22,39]){a.rect(0,y,4,5,'ink');a.rect(1,y,2,4,'mid');a.px(1,y,'tile');}return a.scale(2);
}
function hero(){const a=decode(path.resolve(OUT,'../../../../../Assets/Resources/DraftSprites/hero_idle_4f.png')).crop(0,0,64,64);for(let i=0;i<a.d.length;i+=4){const c=a.d[i+3]>=128?rgba('edge'):[0,0,0,0];for(let k=0;k<4;k++)a.d[i+k]=c[k];}return a;}
function resize(a,w,h){const b=new Raster(w,h);for(let y=0;y<h;y++)for(let x=0;x<w;x++){const p=(Math.floor(y*a.h/h)*a.w+Math.floor(x*a.w/w))*4;b.px(x,y,[...a.d.subarray(p,p+4)]);}return b;}
const POS=ROUND>=4?{body:[96,8],bug:[151,146],blade:[603,0],core:[456,262],hatch:[416,238]}:ROUND>=2?{body:[96,112],bug:[151,250],blade:[603,104],core:[456,366],hatch:[416,342]}:{body:[96,8],bug:[151,139],blade:[603,40],core:[456,262],hatch:[416,238]};
function preview(parts,active=false,full=false,arena=false){
  const a=new Raster(960,arena?540:ROUND>=4?640:ROUND>=2?744:(full?688:540),'#0b0a14');
  for(const [x,y,w]of[[0,301,57],[62,340,35],[854,278,59],[920,318,40]]){a.rect(x,y,w,170,'night');a.rect(x+8,y+17,6,11,'dark');}
  if(ROUND>=2){
    // Muted distant skyline reveals the creature silhouette in the dark arena.
    const off=ROUND>=4?-104:0;
    for(const [x,y,w,h]of[[0,440,60,240],[68,484,39,198],[111,465,40,217],[802,458,52,224],[863,416,44,266],[918,461,42,221]]){a.rect(x,y+off,w,h,'dark');a.rect(x+5,y+off+17,3,13,'mid');a.rect(x+20,y+off+49,3,9,'mid');}
    for(const x of [210,760])a.line([[x,450],[x+6,580],[x+30,680]],'night',8);
  }
  a.blit(parts.boss_body,...POS.body);a.blit(parts.boss_hatch_core,...POS.core);a.blit(parts.boss_hatch_half,POS.hatch[0]-(active?36:0),POS.hatch[1]);a.blit(parts.boss_hatch_half,POS.hatch[0]+64+(active?36:0),POS.hatch[1],true);
  a.blit(parts[active?'boss_bug_arm_lit':'boss_bug_arm'],...POS.bug);
  const sword=parts[active?'boss_blade_arm_lit':'boss_blade_arm'];a.blit(ROUND>=2||full?sword:resize(sword,216,432),POS.blade[0],arena?-62:POS.blade[1]);
  const floor=arena?516:ROUND>=4?578:ROUND>=2?682:(full?626:478);a.rect(0,floor,960,7,'ink');a.rect(0,floor+1,960,2,'steel');a.rect(0,floor+7,960,18,'dark');
  for(let x=0;x<960;x+=48){a.rect(x,floor+7,2,18,'ink');a.rect(x+5,floor+8,37,2,'tile');}a.rect(0,floor+25,960,6,'ink');a.blit(hero(),449,floor-63);return a;
}
function compare(parts){
  if(ROUND<3){const h=ROUND>=2?744:540,a=new Raster(1536,h+32,'#0b0a14');a.blit(source.crop(0,0,756,540),0,ROUND>=2?136:24);a.blit(preview(parts,false).crop(96,0,756,h),780,24);return a;}
  // Both panes use native 1:1 pixels. No per-part resize, no clipped armor.
  const a=new Raster(1944,ROUND>=4?672:776,'#0b0a14');a.blit(source.crop(0,0,756,572),96,ROUND>=4?24:136);a.blit(preview(parts,false),984,24);return a;
}
const parts={boss_body:body(),boss_bug_arm:bug(),boss_bug_arm_lit:bug(true),boss_blade_arm:blade(),boss_blade_arm_lit:blade(true),boss_hatch_half:hatch(),boss_hatch_core:decode(path.join(OUT,'v1/boss_hatch_core.png'))};
fs.mkdirSync(path.join(OUT,'review_v2'),{recursive:true});for(const [n,a]of Object.entries(parts))a.save(n+'.png');
preview(parts).save('preview_1x.png');preview(parts,true).scale(2).save('preview_2x.png');preview(parts,true,true).save('preview_full_1x.png');preview(parts,true).save('review_v2/round_'+ROUND+'_1x.png');compare(parts).save('compare_concept.png');compare(parts).save('review_v2/round_'+ROUND+'_compare.png');
if(ROUND>=4)preview(parts,true,false,true).save('review_v2/arena_960x540.png');
const sheet=new Raster(1136,608,'#0b0a14');for(const [n,x,y]of[['boss_bug_arm',8,16],['boss_bug_arm_lit',216,16],['boss_blade_arm',432,16],['boss_blade_arm_lit',728,16],['boss_hatch_half',1040,20],['boss_hatch_core',1048,145]])sheet.blit(parts[n],x,y);sheet.save('parts_1x.png');
parts.boss_body.crop(260,180,248,210).scale(4).save('review_v2/chest_4x.png');for(const [n,f]of[['boss_bug_arm_lit','bug'],['boss_blade_arm_lit','blade'],['boss_hatch_half','hatch']])parts[n].scale(4).save('review_v2/'+f+'_4x.png');
const union=new Set(),metrics={round:ROUND,parts:{},checks:{}};
for(const [n,a]of Object.entries(parts)){const c=new Set(),alpha=new Set();for(let i=0;i<a.d.length;i+=4){alpha.add(a.d[i+3]);if(a.d[i+3]){const k='#'+a.d.subarray(i,i+3).toString('hex');c.add(k);union.add(k);}}if(c.size>16||[...alpha].some(v=>v!==0&&v!==255))throw Error('Palette/alpha: '+n);if(!decode(path.join(OUT,n+'.png')).d.equals(a.d))throw Error('Roundtrip: '+n);metrics.parts[n]={size:[a.w,a.h],colors:c.size,alpha:[...alpha].sort(),palette:[...c].sort()};}
for(const c of union)if(!Object.values(P).includes(c))throw Error('Unapproved palette');
for(const n of ['boss_bug_arm','boss_blade_arm']){const a=parts[n],b=parts[n+'_lit'];for(let i=3;i<a.d.length;i+=4)if(a.d[i]!==b.d[i])throw Error('Lit silhouette');if(!a.d[((a.h-1)*a.w+a.w/2)*4+3])throw Error('Pivot');}
for(let y=72;y<160;y++)for(let x=312;x<456;x++){const i=(y*768+x)*4;if(!parts.boss_body.d.subarray(i,i+4).equals(Buffer.from(rgba('ink'))))throw Error('Window');}
for(let y=230;y<326;y++)for(let x=320;x<448;x++)if(parts.boss_body.d[(y*768+x)*4+3])throw Error('Hatch');
if(!preview(parts,true).scale(2).d.equals(decode(path.join(OUT,'preview_2x.png')).d))throw Error('Preview2x');
metrics.palette=[...union].sort();metrics.checks={hardAlpha:true,paletteMembership:true,combinedColors:union.size,roundTrip:true,litMaskEqual:true,bottomCenterArmPixels:true,emptyWindow144x88:true,hatchHole128x96:true,exact2xPreview:true};
if(ROUND>=4){
  const assembly=JSON.parse(fs.readFileSync(path.join(OUT,'v1/assembly.json'),'utf8'));
  assembly.status='DRAFT v2 - concept underpainting, four visual review rounds; not imported';
  assembly.parts['boss_blade_arm.png'].registration='Blade tip includes pixel 144,575; bottom-center pivot remains 144,576. Full armor silhouette now fits inside the canvas.';
  assembly.bodyLocal.bugArmTopLeft=[55,138];assembly.bodyLocal.bladeArmTopLeft=[507,-8];
  assembly.preview={nativeSize:[960,640],'preview_1x.png':'Idle arms, closed chest, uncropped native-size parts.','preview_2x.png':'1920x1280, exact 2x nearest-neighbour. Lit arms and open hatch are a review display, not gameplay timing.',arenaBackground:'#0B0A14',bodyTopLeft:POS.body,bugArmTopLeft:POS.bug,bladeArmTopLeft:POS.blade,leftHatchClosedTopLeft:POS.hatch,rightHatchClosedTopLeft:[480,238],hatchOpeningOffsetEachSide:36,coreTopLeft:POS.core,heroCanvasTopLeft:[449,515],heroCanvasSize:[64,64],heroVisibleHeight:59,bridgeTopY:578,drawOrder:['arena','body','core','hatch halves','arms','bridge','hero silhouette'],bladeArmFraming:'All 576 rows shown at native size. Larger proof canvas prevents clipping and fractional scaling.',gameViewportCheck:{file:'review_v2/arena_960x540.png',size:[960,540],bladeTopLeft:[603,-62],heroCanvasTopLeft:[449,453],bridgeTopY:516,note:'Native pixels. Top 62 rows of the huge upper arm extend above the viewport; tip ends y514.'}};
  assembly.comparison={file:'compare_concept.png',size:[1944,672],left:'boss_concept_4.png crop [0,0,756,572] at [96,24]; attack explanation cards omitted',right:'Uncropped 960x640 idle assembly at [984,24]',scale:'Both source and assembly are 1 image pixel per output pixel, without resizing.',note:'Full native blade extends lower than the reference because it is down-pointing and retains requested 288x576 size.'};
  assembly.sourceArt={path:'../boss_concept_4.png',method:'Masked area sampling to half-size authoring grid, 16-color semantic quantization, small-island cleanup, integer pixel redraw, exact 2x export',bodyCrop:[20,0,720,482],bugCrop:[40,112,224,270],clawCrop:[60,343,139,130],armorCrops:[[514,125,180,174],[591,277,125,137],[595,367,108,111]],face:'Removed before quantized body compositing; window is uniformly #080b14.'};
  assembly.limitations=['Asset draft only; Unity imports, .meta pivots, animation and Play/WebGL tests remain unperformed.','PNG files do not embed Unity PPU or pivots. Apply the recorded values when integrating.','Final visual acceptance belongs to the user. The taller proof shows all native pixels; use the 960x540 check for viewport framing.'];
  for(const [n,a]of Object.entries(parts)){const s=assembly.parts[n+'.png'];if(s.size[0]!==a.w||s.size[1]!==a.h)throw Error('Dimensions differ: '+n);}
  const old=JSON.parse(fs.readFileSync(path.join(OUT,'v1/assembly.json'),'utf8'));
  for(const n of Object.keys(assembly.parts))if(JSON.stringify(assembly.parts[n].pivotNormalized)!==JSON.stringify(old.parts[n].pivotNormalized))throw Error('Pivot changed');
  // No crimson or burgundy pixels anywhere in the sword/collar zone.
  for(const n of ['boss_blade_arm','boss_blade_arm_lit']){const a=parts[n];for(let y=374;y<a.h;y++)for(let x=0;x<a.w;x++){const i=(y*a.w+x)*4,k='#'+a.d.subarray(i,i+3).toString('hex');if(a.d[i+3]&&[P.red,P.warm,P.tell].includes(k))throw Error('Red blade stain');}}
  if(union.size!==16)throw Error('Expected original shared 16 colors');
  metrics.checks.sameSizesAndPivotsAsV1=true;metrics.checks.noRedOnBlade=true;metrics.checks.nativeProofWithoutPartScaling=true;
  metrics.preview={native:[960,640],enlarged:[1920,1280],comparison:[1944,672],gameViewport:[960,540]};
  fs.writeFileSync(path.join(OUT,'assembly.json'),JSON.stringify(assembly,null,2)+'\n');
}
fs.writeFileSync(path.join(OUT,'metrics.json'),JSON.stringify(metrics,null,2)+'\n');console.log(JSON.stringify({round:ROUND,checks:metrics.checks},null,2));
