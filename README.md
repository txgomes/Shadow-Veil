# Shadow Veil

Shadow Veil is moving to Unity for a native iPhone game. The project lives in `ShadowVeilUnity/` and targets Unity 6.3 LTS.

## Unity project

Open `ShadowVeilUnity/` in Unity Hub. The initial scene creates a scrolling 3D trail, drag-to-move controls with direction-aware turning, a following camera, simple combat, deployable units, mana cards, and a mobile HUD.

Character bodies are temporary procedural shapes; imported models, rigged animation, pathfinding, and visual effects remain future work. Add the iOS Build Support module to the Unity Editor installation to make an iPhone build. Xcode is required to compile that build locally.

## Browser prototype

The original browser prototype remains in `index.html` and `game.js`; GitHub Pages continues to serve that version.

## Pacto dos Invocadores

O protótipo web do jogo novo fica em `pacto/` e é publicado em https://txgomes.github.io/Shadow-Veil/pacto/. O projeto Unity está na branch `summoner-mvp`.

## Batalha estilo First Summoner (branch `first-summoner`)

A cena `Assets/Scenes/FirstSummoner.unity` é a primeira na build e tem uma fase completa à maneira do First Summoner:

- Arrastas a Rachel pela faixa e ela dispara sozinha. Tocar num inimigo foca-o.
- O baralho tem 6 cartas, cada uma com custo de mana, tempo de recarga, raridade e nível.
- A mana regenera até 30 e cada inimigo morto dá +1.
- A gema azul a meio do mapa dá +20 de mana quando é destruída.
- Ganhas se destruíres o pedestal inimigo antes de acabarem os 5:00. Perdes se o teu cair.
- Quando o pedestal inimigo desce abaixo dos 50%, aparece um boss.
- Há 3 objetivos (diamantes) e um ecrã de resultado com estrelas.

O código está em `Assets/ShadowVeil/Summoner/Scripts/`. As cartas estão em `CardLibrary.cs`, o inimigo em `EnemyDirector.cs` e o HUD em `BattleHud.cs`.

### Arte local (não vai para o Git)

A pasta `Assets/ShadowVeil/Private/` está no `.gitignore`. Põe lá a arte do jogo original:

- `Private/Resources/Characters/<modelId>.prefab`: um prefab com Animator (bool `Moving`, triggers `Attack` e `Death`) substitui o boneco de formas simples. Os ids são `rachel` e o `modelId` de cada carta (`skeleton_archer`, `ogre`, `succubus`…).
- `Private/Resources/CardArt/<cardId>.png` (importado como Sprite): o retrato da carta no HUD.
