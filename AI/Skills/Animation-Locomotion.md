# Skill — Animazione, Animator, IK e locomozione

**Attivazione:** controller player, Blend Tree, clip, Avatar, retargeting, IK, foot sliding/crossing, crouch, camera e movimento.

**Fonti omgskills:** [unity-developer](https://github.com/rmyndharis/antigravity-skills/tree/364ea63c190fcf7cac035f7fa91f0406fa392584), [build-rigged-game-assets](https://github.com/MengTo/Skills/tree/a45892d2fdadab75b31fb16abecc02f101b31c43).

## Fonti locali

Leggere `Documentation/Player-Locomotion.md`, `Humanoid-Rig-Pipeline.md`, `Player-Foot-Separation-IK.md`, `Player-Animation-Asset-Intake-2026-11-20.md`, settings asset, controller, Animator Controller, clip e test.

## Procedura

1. Riprodurre il sintomo su scena/prefab noto.
2. Separare difetto clip sorgente, import, Avatar mapping, retarget, Blend Tree, root motion, Animator, IK e controller.
3. Controllare scale, assi, root transform, foot contact, velocità clip e transizioni.
4. Confrontare clip sorgente e clip sul rig Player: non attribuire al retarget un difetto già presente nella sorgente.
5. Per IK verificare assi anatomici, target, peso, limiti per frame, grounded state e IK Pass.
6. Non usare correzioni runtime per mascherare clip sorgenti difettose; non modificare originali senza richiesta.
7. Scrivere test numerici per invarianti e limiti; usare PlayMode e verifica visiva per la qualità.
8. Provare idle, walk, run, crouch, jump, fall, landing, turn, strafe e cambi direzione.
9. Aggiornare guida con clip, Avatar, parametri e casi testati.

Un test matematico IK non dimostra che i piedi appaiano naturali. Un Avatar importato non dimostra deformazioni corrette. Riportare separatamente test automatici, PlayMode e osservazione visiva.

**Accettazione:** transizioni stabili, nessun difetto evidente nei casi testati, controller non regressivo, parametri configurabili.
