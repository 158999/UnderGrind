# UnderGrind — prima scena di locomozione

## Creazione della scena
1. Aprire il progetto con Unity **6000.6.7f1**.
2. Attendere che Unity termini l'importazione di \`Assets/UnderGrind/Art/Source/humans.blend\`.
3. Dal menu scegliere **UnderGrind → Build First Locomotion Test Scene**.
4. Aprire \`Assets/UnderGrind/Scenes/LocomotionTest.unity\` e premere Play.

Il builder cerca un modello maschile/base nel file Blender, lo scala a 1,78 m e, se trova clip di animazione compatibili, prepara un Animator Controller. Se Unity non riesce a importare il file Blender (ad esempio Blender non è installato o non è configurato), usa un manichino procedurale temporaneo: il file originale resta intatto.

## Parametri iniziali
- Altezza controller: 1,78 m
- Camminata: 1,40 m/s
- Corsa: 1,40 × 4/3 = 1,8667 m/s
- Salto: 0,40 m di altezza teorica (gravità 9,81 m/s²; velocità iniziale calcolata dalla formula balistica)
- Controlli: WASD/frecce, Shift per correre, Spazio per saltare, F5 per passare tra prima e terza persona, Esc per liberare il mouse.

## Scena
La graybox contiene riferimenti di distanza, gradini, ostacoli di altezza misurata, una rampa e piattaforme. L'overlay mostra velocità misurata, velocità nominali, altezza di salto e stato.

## Limiti della prima iterazione
- Le animazioni vengono collegate solo se il file Blender espone clip con nomi riconoscibili (Idle/Standing, Walk, Run, Jump). L'assenza di clip non blocca la locomozione.
- In prima persona vengono nascosti i renderer separati riconoscibili come testa/viso/capelli/occhi. Se il modello usa un'unica mesh skinnata per tutto il corpo, servirà separare la testa o usare una soluzione dedicata alle braccia.
- La scena è una graybox di validazione, non una scena artistica definitiva.
- La creazione dei file via GitHub non equivale a una verifica di importazione o Play Mode: il test runtime deve essere eseguito nell'Editor Unity.
