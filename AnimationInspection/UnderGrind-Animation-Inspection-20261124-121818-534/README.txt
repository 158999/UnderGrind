UNDERGRIND — ANIMATION INSPECTION PACKAGE
Generated UTC: 2026-11-24T12:18:18.5426600Z
Unity: 6000.6.9f1

CONTENTS
- manifest.json: ambiente, opzioni, elenco delle analisi e risultati sintetici.
- index.html: indice navigabile con contact sheet e link ai dati.
- <clip>/<male|female>/report.json: metadati, Avatar, importer, curve, eventi, pose e metriche.
- pose-samples.csv: un record per osso Humanoid e campione temporale, con trasformazioni e derivate.
- transform-hierarchy-samples.csv: un record per ogni Transform del prefab, inclusi nodi non Humanoid, gerarchia, tag/layer, stato attivo, trasformazioni locali/world e derivate cinematiche.
- curve-keys.csv: chiavi di tutte le curve float e object-reference.
- geometry-samples.csv: bounds e statistiche delle mesh skinned campionate ai tempi delle immagini.
- contact-sheet.png: quattro angolazioni per ciascun istante rappresentativo.

CAMPIONAMENTO
- Frequenza uniforme: 30 Hz.
- Frame immagine per clip: 24.
- Dimensione cella: 192 x 320 px.
- Inclusione tempi keyframe: True.
- Limite campioni posa: 12000.
- Diagnostica mesh: True.

LETTURA DELLE MISURE
- Le posizioni/rotazioni world sono misurate sull'istanza temporanea del prefab nel preview scene; quelle local sono relative al parent Transform.
- Velocità e accelerazioni sono differenze finite tra campioni; l'accuratezza dipende dalla frequenza e dai tempi keyframe inclusi.
- Il contatto del piede è una stima basata sull'altezza minima osservata nel ciclo e sulla velocità. La percentuale è una frazione dei campioni, non una durata ponderata; non equivale al grounded state del controller.
- Gli incroci dinamici usano lo stesso riferimento del test Unity: asse laterale ricalcolato a ogni campione dalla differenza LeftUpperLeg - RightUpperLeg, in coordinate locali del root. Un campione è incrociato quando la separazione firmata è inferiore a -crossingToleranceMeters.
- Viene registrata anche la separazione su un asse laterale statico ricavato dalla posa iniziale, così si può distinguere l'incrocio anatomico da un artefatto causato dalla rotazione del bacino.
- Il loop position error confronta la posizione dei piedi relativa alle anche tra t=0 e l'ultimo campione prima della fine (clip.length - 0.0001 s); è riportato separatamente anche l'errore world di ossa selezionate. Alcune clip non sono progettate per chiudersi in posa identica; interpretare insieme a loop settings e immagini.
- Le immagini sono viste statiche renderizzate dal preview scene, non catture del Game View né prova del controller/IK di produzione.
- L'Animator Controller e il root motion sono disattivati nell'anteprima: le curve root motion sono comunque elencate nel CSV delle curve.
- La mesh baked è una diagnostica geometrica, non sostituisce il rendering con materiali/luci di gioco.

LIMITI
- Nessuna interazione, fisica, collisione, foot-separation IK, Blend Tree, transizione o input del Player viene eseguita.
- Il retargeting Humanoid è valutato sui prefab Player, ma non viene eseguita la scena reale.
- Le soglie di contatto sono euristiche configurabili e non sono verdetti automatici di qualità.
- Se il report indica sampleLimitReached=true, i tempi sono stati ridotti per rispettare il limite configurato.
