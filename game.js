(()=>{
'use strict';
const $=id=>document.getElementById(id), canvas=$('world');
let engine;
try{engine=new BABYLON.Engine(canvas,true,{antialias:true,stencil:true,preserveDrawingBuffer:false},true);}
catch(error){$('overlay').querySelector('p').textContent='The 3D renderer could not start. Please open this game in a browser with WebGL enabled and an internet connection.';$('start').disabled=true;console.error('Babylon.js could not create a WebGL engine',error);return;}
const scene=new BABYLON.Scene(engine);
scene.clearColor=new BABYLON.Color4(.035,.027,.045,1);
scene.ambientColor=new BABYLON.Color3(.45,.42,.48);
const camera=new BABYLON.ArcRotateCamera('trail-camera',-Math.PI/2,.34,30,BABYLON.Vector3.Zero(),scene);
camera.mode=BABYLON.Camera.ORTHOGRAPHIC_CAMERA;
camera.lowerRadiusLimit=30;camera.upperRadiusLimit=30;camera.inputs.clear();
scene.activeCamera=camera;
const light=new BABYLON.HemisphericLight('moonlight',new BABYLON.Vector3(-.2,1,-.3),scene);light.intensity=1.05;
const warmLight=new BABYLON.PointLight('lantern-glow',new BABYLON.Vector3(-2,4,-1),scene);warmLight.diffuse=new BABYLON.Color3(1,.49,.19);warmLight.intensity=5;warmLight.range=10;
const coolLight=new BABYLON.PointLight('mist-glow',new BABYLON.Vector3(2,3,2),scene);coolLight.diffuse=new BABYLON.Color3(.16,.55,.47);coolLight.intensity=2.5;coolLight.range=8;
const ppu=()=>Math.max(1,W/8), tiltCos=Math.cos(.34);
const mat=(name,color,emissive)=>{const m=new BABYLON.StandardMaterial(name,scene);m.diffuseColor=BABYLON.Color3.FromHexString(color);m.specularColor=new BABYLON.Color3(.13,.13,.13);if(emissive)m.emissiveColor=BABYLON.Color3.FromHexString(emissive);return m;};
const mats={
ground:null,wood:mat('Reliquary wood','#49332c','#180e0b'),
iron:mat('Blackened iron','#3b414b','#10151c'),
bone:mat('Old bone','#c8bfae','#332d27'),
boneShade:mat('Bone shadows','#665a59','#161117'),
skin:mat('Warm skin','#d69478','#2b100c'),
hair:mat('Ash red hair','#8f3c35','#270d0b'),
leather:mat('Dark leather','#302a30','#100c12'),
gold:mat('Old gold','#c39a51','#3e230e'),
teal:mat('Verdigris cloth','#557f72','#14231e'),
red:mat('Wraith red','#aa435d','#40101b'),
ember:mat('Ember','#ed794e','#8c2912'),
shadow:mat('Actor shadow','#17141c','#07070a'),
health:mat('Health cache','#4c8b70','#5ad49b'),
mana:mat('Mana cache','#315b88','#58b8f1'),
soul:mat('Soul cache','#98733b','#e1bd65'),
greenBar:mat('Health bar','#76c895','#376d41'),
redBar:mat('Enemy health bar','#da5263','#872635'),
white:mat('Rune glow','#e8d9bd','#c7ad78')
};
const characterMaterials=new Map();
let heroMaterialCache=null;
function characterMaterial(index){
 let material=characterMaterials.get(index);if(material)return material;
 const col=index%4,row=Math.floor(index/4),texture=new BABYLON.Texture('assets/character-atlas.png',scene,false,true,BABYLON.Texture.TRILINEAR_SAMPLINGMODE);
 texture.uScale=.25;texture.vScale=.5;texture.uOffset=col*.25;texture.vOffset=(1-row)*.5;texture.wrapU=BABYLON.Texture.CLAMP_ADDRESSMODE;texture.wrapV=BABYLON.Texture.CLAMP_ADDRESSMODE;texture.hasAlpha=true;
 material=new BABYLON.StandardMaterial('character art '+index,scene);material.diffuseTexture=texture;material.opacityTexture=texture;material.useAlphaFromDiffuseTexture=true;material.transparencyMode=BABYLON.Material.MATERIAL_ALPHABLEND;material.disableLighting=true;material.emissiveColor=new BABYLON.Color3(.82,.82,.82);material.specularColor=BABYLON.Color3.Black();material.backFaceCulling=false;material.disableDepthWrite=true;characterMaterials.set(index,material);return material;
}
function heroMaterial(){
 if(heroMaterialCache)return heroMaterialCache;
 const texture=new BABYLON.Texture('assets/ranger-hero.webp',scene,false,true,BABYLON.Texture.TRILINEAR_SAMPLINGMODE);
 texture.hasAlpha=true;
 heroMaterialCache=new BABYLON.StandardMaterial('Ashen Ranger portrait',scene);heroMaterialCache.diffuseTexture=texture;heroMaterialCache.opacityTexture=texture;heroMaterialCache.useAlphaFromDiffuseTexture=true;heroMaterialCache.transparencyMode=BABYLON.Material.MATERIAL_ALPHABLEND;heroMaterialCache.disableLighting=true;heroMaterialCache.emissiveColor=new BABYLON.Color3(.9,.9,.9);heroMaterialCache.specularColor=BABYLON.Color3.Black();heroMaterialCache.backFaceCulling=false;heroMaterialCache.disableDepthWrite=true;
 return heroMaterialCache;
}
const groundRoot=new BABYLON.TransformNode('endless-trail',scene),groundMeshes=[];
let trailTexture=new BABYLON.Texture('assets/forest-battlefield.webp',scene,false,true,BABYLON.Texture.TRILINEAR_SAMPLINGMODE);
trailTexture.wrapU=BABYLON.Texture.WRAP_ADDRESSMODE;trailTexture.wrapV=BABYLON.Texture.WRAP_ADDRESSMODE;trailTexture.uScale=1;trailTexture.vScale=1;
const floorMat=new BABYLON.StandardMaterial('forest-trail-material',scene);floorMat.diffuseTexture=trailTexture;floorMat.emissiveColor=new BABYLON.Color3(.16,.14,.13);floorMat.specularColor=BABYLON.Color3.Black();
for(let i=0;i<5;i++){const tile=BABYLON.MeshBuilder.CreateGround('trail-tile-'+i,{width:8.2,height:12,subdivisions:1},scene);tile.position.z=(i-2)*12;tile.material=floorMat;tile.parent=groundRoot;tile.isPickable=true;groundMeshes.push(tile);}
const edgeMat=mat('Trail edge','#171c1a');
for(const side of [-1,1]){const wall=BABYLON.MeshBuilder.CreateBox('forest-bank',{width:1.4,height:.85,depth:64},scene);wall.position.set(side*4.65,.36,0);wall.material=edgeMat;wall.parent=groundRoot;}
const ringMat=mat('Movement marker','#d6b477','#6b411c');ringMat.alpha=.8;
const marker=BABYLON.MeshBuilder.CreateTorus('movement-destination',{diameter:.5,thickness:.035,tessellation:24},scene);marker.material=ringMat;marker.rotation.x=Math.PI/2;marker.isVisible=false;
const spellMat=mat('Spell target','#b866ec','#772ca6');spellMat.alpha=.24;
const spellRingMat=mat('Spell target edge','#db9aff','#8a38ca');
const spellDisc=BABYLON.MeshBuilder.CreateDisc('spell-area',{radius:1,tessellation:48},scene);spellDisc.rotation.x=Math.PI/2;spellDisc.material=spellMat;spellDisc.isVisible=false;
const spellRing=BABYLON.MeshBuilder.CreateTorus('spell-area-outline',{diameter:2,thickness:.035,tessellation:48},scene);spellRing.rotation.x=Math.PI/2;spellRing.material=spellRingMat;spellRing.isVisible=false;
const actorViews=new Map(),crateViews=new Map(),particleViews=new Map(),boltViews=new Map(),particleMaterials=new Map();
function applyViewport(){const r=canvas.getBoundingClientRect();W=r.width;H=r.height;engine.resize();const hw=W/(2*ppu()),hh=H/(2*ppu());camera.orthoLeft=-hw;camera.orthoRight=hw;camera.orthoTop=hh;camera.orthoBottom=-hh;}
let W=0,H=0,last=0,running=false,ended=false,elapsed=0,timeLeft=210,spawnTimer=0,crateTimer=5,kills=0,wave=1,souls=0,cameraY=0,player,allies=[],enemies=[],particles=[],bolts=[],crates=[],goal=null,focus=null,selectedSpell=null,aimPoint=null,bossSpawned=false,dragging=false;
const cards=[
{id:'guard',name:'Graveguard',symbol:'☠',desc:'Tank · melee',cost:5,type:'unit',hp:92,damage:9,range:31,speed:42,rate:.8,color:'#d8c9d5',role:'tank',sprite:1,artIndex:1,art:'33.333% 0%'},
{id:'ranger',name:'Ashen Archer',symbol:'➶',desc:'Ranged damage',cost:8,type:'unit',hp:42,damage:10,range:150,speed:42,rate:1.05,color:'#a4d6c8',role:'ranged',sprite:2,artIndex:2,art:'66.666% 0%'},
{id:'shade',name:'Gloam Imp',symbol:'♆',desc:'Fast · fierce',cost:5,type:'unit',hp:52,damage:17,range:36,speed:76,rate:.58,color:'#e78c77',role:'assault',sprite:3,artIndex:3,art:'100% 0%'},
{id:'oracle',name:'Lantern Seer',symbol:'✺',desc:'Healing · support',cost:18,type:'unit',hp:48,damage:4,range:115,speed:35,rate:1.2,color:'#dfc17e',role:'healer',sprite:4,artIndex:4,art:'0% 100%'},
{id:'ember',name:'Cinderburst',symbol:'✧',desc:'Spell · blast',cost:2,type:'spell',spell:'ember',color:'#ff9a72',art:'66.666% 100%'},
{id:'frost',name:'Rime Veil',symbol:'❄',desc:'Spell · slow',cost:5,type:'spell',spell:'frost',color:'#8ce9ed',art:'33.333% 100%'}
];
function buildDeck(){$('deck').innerHTML=cards.map(c=>'<button class="card" data-card="'+c.id+'" aria-label="'+c.name+', costs '+c.cost+' mana"><span class="card-art" style="--art-pos:'+c.art+'"></span><span class="cost">'+c.cost+'</span><div class="symbol">'+c.symbol+'</div><div class="name">'+c.name+'</div></button>').join('');$('deck').querySelectorAll('.card').forEach(b=>b.addEventListener('click',()=>playCard(b.dataset.card)));}
function clamp(v,a,b){return Math.max(a,Math.min(b,v));}function dist(a,b){return Math.hypot(a.x-b.x,a.y-b.y);}
function to3(x,y,groundY=0){const s=ppu();return new BABYLON.Vector3((x-W/2)/s,groundY,(H/2-y)/(s*tiltCos));}
function resize(){applyViewport();if(player){player.x=clamp(player.x,24,W-24);player.y=clamp(player.y,cameraY+112,cameraY+H-174);}}
function toast(t){const el=$('toast');el.textContent=t;el.style.opacity=1;clearTimeout(toast.timer);toast.timer=setTimeout(()=>el.style.opacity=0,1450);}
function reset(){player={x:W/2,y:H*.68,r:12,hp:100,maxHp:100,mana:12,maxMana:26,speed:152,invuln:0,shot:0,sprite:2,hero:true};allies=[];enemies=[];particles=[];bolts=[];crates=[];elapsed=0;timeLeft=210;cameraY=0;spawnTimer=.7;crateTimer=5;kills=0;wave=1;souls=0;bossSpawned=false;focus=null;selectedSpell=null;aimPoint=null;goal=null;ended=false;running=true;$('overlay').style.display='none';toast('The mist is moving…');updateHud();}
function nearest(from,range){let t=null,b=range||Infinity;for(const e of enemies){const d=dist(from,e);if(d<b){b=d;t=e;}}return t;}
function spawnEnemy(kind){const x=W*.2+Math.random()*W*.6,boss=kind==='boss',tough=boss||Math.random()<.18+wave*.035,hp=boss?360:(tough?64+wave*7:31+wave*3);enemies.push({x:x,y:cameraY+H*(.18+Math.random()*.1),r:boss?25:(tough?15:11),hp:hp,maxHp:hp,damage:boss?18:(tough?12:7),speed:boss?17:(tough?24+wave:31+wave),attackCd:.5+Math.random(),color:boss?'#b74664':(tough?'#bd687c':'#8171a0'),tough:tough,boss:boss,slow:0,sprite:boss?7:(tough?6:5),artIndex:boss?6:(tough?5:1),attackPulse:0,summonTime:.36});}
function spawnCrate(){const roll=Math.random(),type=roll<.4?'health':roll<.75?'mana':'soul';crates.push({x:W*.18+Math.random()*W*.64,y:cameraY+H*.24,type:type,r:13,phase:Math.random()*6});}
function playCard(id){if(!running||ended)return;const c=cards.find(x=>x.id===id);if(!c)return;if(selectedSpell&&selectedSpell.id===id){selectedSpell=null;aimPoint=null;toast('Spell cancelled');updateHud();return;}if(player.mana<c.cost){toast('Not enough mana');return;}if(c.type==='spell'){selectedSpell=c;aimPoint={x:player.x,y:player.y};toast('Tap the battlefield to aim '+c.name);updateHud();return;}selectedSpell=null;aimPoint=null;if(allies.length>=7){toast('Seven allies already on the trail');return;}player.mana-=c.cost;const a=Object.assign({},c,{x:player.x,y:player.y,r:c.role==='tank'?12:10,cd:.25,maxHp:c.hp,slow:0,attackPulse:0,summonTime:.42});const slots=[[-96,-38],[96,-38],[-112,24],[112,24],[-80,86],[80,86],[0,-118]];let spot=slots.find(([x,y])=>![player,...allies].some(u=>Math.hypot(u.x-(player.x+x),u.y-(player.y+y))<Math.max(74,u.r+30)));if(!spot)spot=slots[allies.length%slots.length];a.x=clamp(player.x+spot[0],25,W-25);a.y=player.y+spot[1];allies.push(a);burst(a.x,a.y,c.color,10);toast(c.name+' summoned');updateHud();}
function castSpell(c,center){const radius=c.spell==='ember'?66:84,target=nearest(center,radius);if(target)bolts.push({x:player.x,y:player.y,target:target,life:.26,color:c.color});burst(center.x,center.y,c.color,18);for(const e of [...enemies])if(dist(e,center)<radius){if(c.spell==='ember'){e.hp-=78;burst(e.x,e.y,c.color,12);if(e.hp<=0)killEnemy(e);}else{e.slow=3.2;burst(e.x,e.y,c.color,6);}}if(c.spell==='frost')for(const a of allies)if(dist(a,center)<radius)a.hp=Math.min(a.maxHp,a.hp+8);toast(c.spell==='ember'?'Cinderburst!':'Rime Veil slows the wraiths');}
function breakCrate(c){const i=crates.indexOf(c);if(i<0)return;crates.splice(i,1);if(c.type==='health'){const restored=Math.min(30,player.maxHp-player.hp);player.hp=Math.min(player.maxHp,player.hp+30);toast(restored?'+30 HP':'+5 mana');if(!restored)player.mana=Math.min(player.maxMana,player.mana+5);}else if(c.type==='mana'){player.mana=Math.min(player.maxMana,player.mana+8);toast('+8 mana');}else{souls+=3;toast('+3 souls');}burst(c.x,c.y,c.type==='health'?'#8bcbb9':c.type==='mana'?'#75d8dc':'#d6b477',12);updateHud();}
function killEnemy(e){const i=enemies.indexOf(e);if(i<0)return;enemies.splice(i,1);if(e.boss){souls+=12;burst(e.x,e.y,e.color,34);endGame(true);return;}kills++;souls+=e.tough?3:1;player.mana=Math.min(player.maxMana,player.mana+(e.tough?7:4));burst(e.x,e.y,e.color,13);const n=Math.min(3,1+Math.floor(kills/5));if(n>wave){wave=n;toast('Trail '+wave+' · deeper into the mist');}if(kills>=15&&!bossSpawned){bossSpawned=true;spawnEnemy('boss');toast('The Ashen Bellkeeper appears!');}updateHud();}
function updateHud(){if(!player)return;$('hpText').textContent=Math.ceil(player.hp)+' / '+player.maxHp;$('manaText').textContent=Math.floor(player.mana)+' / '+player.maxMana;$('hpFill').style.width=100*player.hp/player.maxHp+'%';$('manaFill').style.width=100*player.mana/player.maxMana+'%';$('stage').textContent=bossSpawned?'BELLKEEPER':'PATH '+wave;$('souls').textContent='◇ '+souls;$('objective').textContent=selectedSpell?'Choose a spell target · Tap card again to cancel':bossSpawned?'Defeat the Ashen Bellkeeper':'Wraiths: '+kills+'/15 · Allies: '+allies.length;const target=focus&&enemies.includes(focus)?focus:(enemies[0]||null);$('enemyName').textContent=target?(target.boss?'ASHEN BELLKEEPER':target.tough?'ELITE SKELETON':'SKELETON WARRIOR'):'WAITING FOR ENEMIES';$('enemyLevel').textContent=target?'LV. '+(target.boss?3:target.tough?2:1):'';$('enemyFill').style.width=target?100*clamp(target.hp/target.maxHp,0,1)+'%':'0%';const seconds=Math.ceil(timeLeft);$('timer').textContent='◷ '+String(Math.floor(seconds/60)).padStart(2,'0')+':'+String(seconds%60).padStart(2,'0');document.querySelectorAll('.card').forEach(b=>{const c=cards.find(x=>x.id===b.dataset.card);b.classList.toggle('targeting',selectedSpell?.id===c.id);b.disabled=player.mana<c.cost||(c.type==='unit'&&allies.length>=7);});}
function moveToward(a,b,speed,dt){const dx=b.x-a.x,dy=b.y-a.y,d=Math.hypot(dx,dy);if(d>4){const step=Math.min(speed*dt,d);a.x+=dx/d*step;a.y+=dy/d*step;a.walking=true;}else a.walking=false;}
function burst(x,y,color,n){for(let i=0;i<n;i++){const a=Math.random()*Math.PI*2,s=18+Math.random()*85;particles.push({x:x,y:y,vx:Math.cos(a)*s,vy:Math.sin(a)*s,life:.25+Math.random()*.45,color:color,r:1+Math.random()*2});}}
function update(dt){if(!running||ended)return;elapsed+=dt;timeLeft-=dt;if(timeLeft<=0){timeLeft=0;endGame(false);return;}player.mana=Math.min(player.maxMana,player.mana+1.8*dt);player.invuln=Math.max(0,player.invuln-dt);player.shot-=dt;player.walking=false;for(const a of allies)a.walking=false;for(const e of enemies)e.walking=false;if(goal){moveToward(player,goal,player.speed,dt);if(dist(player,goal)<7)goal=null;}player.x=clamp(player.x,24,W-24);player.y=clamp(player.y,cameraY+105,cameraY+H-176);const screenY=player.y-cameraY;if(screenY<H*.38)cameraY=player.y-H*.38;else if(screenY>H*.68)cameraY=player.y-H*.68;
spawnTimer-=dt;if(spawnTimer<=0&&!bossSpawned&&enemies.length<5){spawnEnemy();spawnTimer=Math.max(1,2.4-wave*.3);}if(bossSpawned&&enemies.length===0&&!ended)spawnEnemy('boss');crateTimer-=dt;if(crateTimer<=0){spawnCrate();crateTimer=8+Math.random()*4;}
for(const c of crates){c.y+=20*dt;c.phase+=dt;}crates=crates.filter(c=>c.y-cameraY<H+30&&c.y-cameraY>-60);
for(const a of [...allies]){a.cd-=dt;const t=nearest(a,a.range+70);if(a.role==='healer'){const hurt=[player,...allies].filter(x=>x.hp<x.maxHp-2).sort((x,y)=>x.hp/x.maxHp-y.hp/y.maxHp)[0];if(hurt&&dist(a,hurt)<a.range&&a.cd<=0){hurt.hp=Math.min(hurt.maxHp,hurt.hp+11);a.cd=a.rate;a.attackPulse=.22;burst(hurt.x,hurt.y,'#e7d28f',4);}else if(t){if(dist(a,t)>a.range*.78)moveToward(a,t,a.speed,dt);else if(a.cd<=0){t.hp-=a.damage;a.cd=a.rate;a.attackPulse=.26;if(t.hp<=0)killEnemy(t);}}}else if(t){if(dist(a,t)>a.range-5)moveToward(a,t,a.speed,dt);else if(a.cd<=0){t.hp-=a.damage;a.cd=a.rate;a.attackPulse=.26;burst(t.x,t.y,a.color,3);if(t.hp<=0)killEnemy(t);}}}
if(player.shot<=0){const t=focus&&enemies.includes(focus)?focus:nearest(player,220);if(t){t.hp-=8;player.shot=.78;player.attackPulse=.22;bolts.push({x:player.x,y:player.y,target:t,life:.18,color:'#c6a8e8'});if(t.hp<=0)killEnemy(t);}}
for(const e of [...enemies]){e.slow=Math.max(0,e.slow-dt);const t=[player,...allies].sort((a,b)=>dist(e,a)-dist(e,b))[0],range=e.r+t.r+3;if(dist(e,t)>range)moveToward(e,t,e.speed*(e.slow?.42:1),dt);else{e.attackCd-=dt;if(e.attackCd<=0){t.hp-=e.damage;e.attackCd=e.boss?1.15:1;e.attackPulse=.28;burst(t.x,t.y,'#d85c65',4);if(t===player)player.invuln=.22;if(t.hp<=0){if(t===player){endGame(false);return;}const i=allies.indexOf(t);if(i>=0)allies.splice(i,1);}}}}
for(const p of particles){p.x+=p.vx*dt;p.y+=p.vy*dt;p.vx*=.96;p.vy*=.96;p.life-=dt;}particles=particles.filter(p=>p.life>0);bolts.forEach(b=>b.life-=dt);bolts=bolts.filter(b=>b.life>0);updateHud();}

function createPart(parent,kind,name,opts,material){const mesh=BABYLON.MeshBuilder[kind](name,opts,scene);mesh.parent=parent;mesh.material=material;mesh.isPickable=false;return mesh;}
function limb(parent,name,material,length=.48,radius=.09){const pivot=new BABYLON.TransformNode(name+' pivot',scene);pivot.parent=parent;const mesh=createPart(pivot,'CreateCylinder',name,{height:length,diameterTop:radius*1.55,diameterBottom:radius*1.9,tessellation:7},material);mesh.position.y=-length*.48;const cuff=createPart(pivot,'CreateSphere',name+' joint',{diameter:radius*2.4,segments:6},material);cuff.position.y=.02;return pivot;}
function createActorModel(a){
 const root=new BABYLON.TransformNode('actor '+(a.name||a.role||'hero'),scene);
 const shadow=createPart(root,'CreateSphere','soft ground shadow',{diameter:1,segments:12},mats.shadow);shadow.scaling.set(.5,.045,.29);shadow.position.set(0,.04,0);
 const height=a.boss?2.85:a.role==='assault'?1.45:a.hero?1.82:2.08,width=height*(a.hero?1024/1536:.72);
 const sprite=BABYLON.MeshBuilder.CreatePlane('illustrated character',{width,height},scene);sprite.parent=root;sprite.position.y=height*.51;sprite.billboardMode=BABYLON.Mesh.BILLBOARDMODE_ALL;sprite.material=a.hero?heroMaterial():characterMaterial(a.artIndex??1);sprite.isPickable=false;
 const barBack=createPart(root,'CreateBox','health bar frame',{width:.68,height:.055,depth:.035},mats.boneShade);barBack.position.set(0,height+.17,0);
 const bar=createPart(root,'CreateBox','health bar fill',{width:.64,height:.033,depth:.04},a.hero||a.role?mats.greenBar:mats.redBar);bar.position.set(0,height+.17,-.025);
 const focusRing=BABYLON.MeshBuilder.CreateTorus('target ring',{diameter:.78,thickness:.035,tessellation:20},scene);focusRing.parent=root;focusRing.position.y=.08;focusRing.rotation.x=Math.PI/2;focusRing.material=ringMat;focusRing.isPickable=false;focusRing.isVisible=false;
 return{root,sprite,shadow,bar,focusRing,height,width,phase:Math.random()*5,lastX:a.x,lastY:a.y,walkBlend:0,stepTimer:Math.random()*.12,facingX:1,facingY:0};
}
function ensureActor(a){let v=actorViews.get(a);if(!v){v=createActorModel(a);actorViews.set(a,v);}return v;}
function createCrateModel(c){const root=new BABYLON.TransformNode('reliquary '+c.type,scene),m=c.type==='health'?mats.health:c.type==='mana'?mats.mana:mats.soul;
 const box=createPart(root,'CreateBox','sealed cache',{width:.48,height:.42,depth:.48},mats.wood);box.position.y=.23;
 const lid=createPart(root,'CreateBox','cache lid',{width:.53,height:.13,depth:.53},m);lid.position.y=.48;
 const band=createPart(root,'CreateBox','cache band',{width:.09,height:.38,depth:.55},mats.gold);band.position.set(0,.32,-.01);
 const rune=BABYLON.MeshBuilder.CreatePlane('cache sigil',{size:.28},scene);rune.parent=root;rune.position.set(0,.7,-.035);rune.billboardMode=BABYLON.Mesh.BILLBOARDMODE_ALL;
 const tex=new BABYLON.DynamicTexture('cache glyph',{width:128,height:128},scene,false);const c2=tex.getContext();c2.clearRect(0,0,128,128);c2.font='bold 86px serif';c2.textAlign='center';c2.textBaseline='middle';c2.fillStyle=c.type==='health'?'#9df2c8':c.type==='mana'?'#8be1ff':'#ffe19a';c2.shadowColor=c2.fillStyle;c2.shadowBlur=18;c2.fillText(c.type==='health'?'+':c.type==='mana'?'✦':'◇',64,64);tex.update();const rm=new BABYLON.StandardMaterial('cache glyph material',scene);rm.diffuseTexture=tex;rm.opacityTexture=tex;rm.emissiveColor=new BABYLON.Color3(.75,.65,.45);rm.disableLighting=true;rm.backFaceCulling=false;rune.material=rm;
 return{root,rune};}
function createParticle(p){const mesh=BABYLON.MeshBuilder.CreateSphere('spell mote',{diameter:.08+Math.random()*.06,segments:5},scene);let m=particleMaterials.get(p.color);if(!m){m=mat('mote '+p.color,p.color,p.color);particleMaterials.set(p.color,m);}mesh.material=m;return mesh;}
function syncScene(dt){
 const s=ppu(),cameraZ=-cameraY/(s*tiltCos),tileIndex=Math.floor(cameraZ/12);camera.target.z=cameraZ;groundRoot.position.z=tileIndex*12;
groundMeshes.forEach((tile,i)=>{tile.position.z=(i-2)*12;});
 const actors=[player,...allies,...enemies].filter(Boolean),activeActors=new Set(actors);
 for(const [a,v] of actorViews)if(!activeActors.has(a)){v.root.dispose(false,false);actorViews.delete(a);}
 for(const a of actors){const v=ensureActor(a),pos=to3(a.x,a.y,0);v.root.position.x=pos.x;v.root.position.z=pos.z;v.root.position.y=.03;
  const dx=a.x-v.lastX,dy=a.y-v.lastY,motion=Math.hypot(dx,dy),moved=motion>.12,isMoving=moved||a.walking;if(moved){v.facingX=dx/motion;v.facingY=dy/motion;}v.lastX=a.x;v.lastY=a.y;v.walkBlend+=(clamp(isMoving?1:0,0,1)-v.walkBlend)*Math.min(1,dt*8);v.phase+=dt*(isMoving?11:2.5);
  const attack=a.attackPulse>0?Math.sin((.3-a.attackPulse)*Math.PI/.3):0;
  if(a.attackPulse>0)a.attackPulse=Math.max(0,a.attackPulse-dt);
  const summon=clamp(1-(a.summonTime||0)/.42,0,1),pop=.28+.72*summon*summon*(3-2*summon),stride=Math.sin(v.phase),hop=a.role==='assault'?.3:.22;
  a.summonTime=Math.max(0,(a.summonTime||0)-dt);v.sprite.visibility=.35+.65*summon;
  v.sprite.position.x=Math.sin(v.phase)*.06*v.walkBlend;
  v.sprite.position.y=v.height*.51*pop+Math.abs(stride)*hop*v.walkBlend+Math.sin(v.phase)*(.018+(a.role==='healer'?.05:0))*(1-v.walkBlend)+attack*.1;
  v.sprite.rotation.x=-v.facingY*.17*v.walkBlend;
  v.sprite.rotation.z=v.facingX*.18*v.walkBlend+Math.sin(v.phase)*(.08*v.walkBlend+(a.role==='healer'?.025:0)*(1-v.walkBlend))+attack*.2;
  const facing=v.facingX<-.18?-1:1;v.sprite.scaling.x=facing*pop*(1+attack*.12+Math.abs(stride)*.055*v.walkBlend);v.sprite.scaling.y=pop*(1-attack*.07-Math.max(0,-stride)*.065*v.walkBlend);v.shadow.scaling.x=.5+v.walkBlend*.12;v.shadow.scaling.z=.29+v.walkBlend*.08;
  v.stepTimer-=dt;if(isMoving&&v.stepTimer<=0){for(let puff=0;puff<2;puff++)particles.push({x:a.x+(Math.random()-.5)*14,y:a.y+a.r*.35,vx:(Math.random()-.5)*24,vy:-8-Math.random()*12,life:.22+Math.random()*.12,color:'#9b8a72',r:10+Math.random()*6});v.stepTimer=.27;}
  v.root.position.y=a.invuln>0&&Math.floor(elapsed*18)%2?.1:.03;
  v.bar.scaling.x=Math.max(.02,.64*clamp(a.hp/a.maxHp,0,1));v.bar.position.x=-(.64-v.bar.scaling.x)*.5;
  v.focusRing.isVisible=focus===a;v.focusRing.rotation.z=elapsed*.65;
 }
 const activeCrates=new Set(crates);for(const [c,v] of crateViews)if(!activeCrates.has(c)){v.root.dispose(false,false);if(v.rune.material)v.rune.material.dispose(true,true);crateViews.delete(c);}
 for(const c of crates){let v=crateViews.get(c);if(!v){v=createCrateModel(c);crateViews.set(c,v);}const pos=to3(c.x,c.y,.03);v.root.position.set(pos.x,.03+Math.sin(c.phase*2)*.045,pos.z);v.root.rotation.y=Math.sin(c.phase)*.08;}
 const activeParticles=new Set(particles);for(const [p,m] of particleViews)if(!activeParticles.has(p)){m.dispose(false,false);particleViews.delete(p);}
 for(const p of particles){let m=particleViews.get(p);if(!m){m=createParticle(p);particleViews.set(p,m);}const pos=to3(p.x,p.y,.12+Math.max(0,.55-p.life)*.55);m.position.copyFrom(pos);m.scaling.setAll(Math.max(.08,p.r/13));m.visibility=clamp(p.life/.7,0,1);}
 const activeBolts=new Set(bolts);for(const [b,m] of boltViews)if(!activeBolts.has(b)){m.dispose();boltViews.delete(b);}
 for(const b of bolts){const start=to3(b.x,b.y,.65),end=to3(b.target.x,b.target.y,.75);let line=boltViews.get(b);if(line)line=BABYLON.MeshBuilder.CreateLines('arcane bolt',{points:[start,end],instance:line});else line=BABYLON.MeshBuilder.CreateLines('arcane bolt',{points:[start,end]},scene);line.color=BABYLON.Color3.FromHexString(b.color);line.alpha=.9;boltViews.set(b,line);}
 if(goal){const p=to3(goal.x,goal.y,.08);marker.position.copyFrom(p);marker.isVisible=true;marker.rotation.y=elapsed*.5;}else marker.isVisible=false;
 if(selectedSpell&&aimPoint){const p=to3(aimPoint.x,aimPoint.y,.07),radius=(selectedSpell.spell==='ember'?66:84)/s;spellDisc.position.copyFrom(p);spellRing.position.copyFrom(p);spellDisc.scaling.set(radius,radius,radius);spellRing.scaling.setAll(radius);spellDisc.isVisible=true;spellRing.isVisible=true;spellRing.rotation.z=elapsed*.35;}else{spellDisc.isVisible=false;spellRing.isVisible=false;}
}
function endGame(win){ended=true;running=false;$('overlay').style.display='flex';$('overlay').querySelector('h1').textContent=win?'THE TRAIL BREAKS':'ASHES CLAIM YOU';$('overlay').querySelector('p').textContent=win?'The Bellkeeper has fallen. The bells are silent — for now.':'Your flame has gone out. The trail still hides another path.';$('overlay').querySelectorAll('p')[1].textContent='Wraiths defeated: '+kills+' · Souls gathered: '+souls;$('start').textContent='TRY AGAIN';}
function point(e){const r=canvas.getBoundingClientRect();return{x:e.clientX-r.left,y:e.clientY-r.top};}
canvas.addEventListener('pointerdown',e=>{if(!running||ended)return;e.preventDefault();dragging=true;canvas.setPointerCapture(e.pointerId);const p=point(e),world={x:p.x,y:p.y+cameraY};if(selectedSpell){const c=selectedSpell;selectedSpell=null;aimPoint=null;player.mana-=c.cost;castSpell(c,world);updateHud();return;}const box=crates.find(c=>dist(c,world)<c.r+12);if(box){breakCrate(box);return;}const t=[...enemies].reverse().find(a=>dist(a,world)<a.r+18);if(t){focus=t;toast(t.boss?'Focus on the Bellkeeper':'Wraith targeted');return;}focus=null;goal={x:clamp(p.x,24,W-24),y:clamp(p.y+cameraY,cameraY+110,cameraY+H-176)};});
canvas.addEventListener('pointermove',e=>{if(!running)return;const p=point(e);if(selectedSpell){aimPoint={x:p.x,y:p.y+cameraY};return;}if(!dragging)return;if(p.y<H-165)goal={x:clamp(p.x,24,W-24),y:clamp(p.y+cameraY,cameraY+110,cameraY+H-176)};});canvas.addEventListener('pointerup',()=>dragging=false);canvas.addEventListener('pointercancel',()=>dragging=false);
$('start').addEventListener('click',reset);window.addEventListener('resize',resize);buildDeck();resize();player={x:W/2,y:H*.68,r:12,hp:100,maxHp:100,mana:12,maxMana:26,speed:152,invuln:0,shot:0,sprite:2,hero:true,attackPulse:0};updateHud();
engine.runRenderLoop(()=>{const now=performance.now(),dt=Math.min(.035,(now-last)/1000||0);last=now;update(dt);syncScene(dt);scene.render();});
})();
