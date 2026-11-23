# Correzione dell'incrocio dei piedi

## Sintomo rilevato

Il test `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars` del run `20261123-122409-288` rileva incroci già nelle clip Mixamo sorgenti. I valori minimi anatomici riportati sono -0,059 m per Running e -0,114 m per Crouch Walk Forward. Il retarget aumenta il fenomeno sui rig Player.

## Correzione runtime

`PlayerFootSeparationIK` applica una correzione limitata ai target IK dei piedi quando la distanza laterale anatomica scende sotto il valore minimo configurato. L'asse laterale viene ricalcolato a ogni valutazione dalle anche sinistra e destra, quindi segue la rotazione del bacino.

Parametri configurabili nell'Inspector:
- `Minimum Foot Separation`: distanza laterale minima desiderata, in metri.
- `Correction Weight`: peso della correzione, da 0 a 1.
- `Maximum Correction Per Foot`: spostamento massimo per ciascun piede, in metri.
- `Only When Grounded`: limita l'intervento alla fase a terra.
- `Grounded Parameter Name`: parametro booleano dell'Animator usato per riconoscere il contatto a terra.

## Attivazione

Selezionare il GameObject che contiene l'Animator del personaggio e usare `UnderGrind > Player > Enable Foot Separation IK on Selected Animator`. Il comando abilita IK Pass sui layer del controller e aggiunge il componente selezionato. Salvare il prefab dopo aver verificato il risultato.

La correzione è intenzionalmente limitata e non modifica le clip sorgenti. Può ridurre l'incrocio dei piedi, ma non sostituisce la revisione delle animazioni: va verificata in Play Mode con camminata, corsa, accovacciamento, curve e cambi di direzione.

## Verifica

I test unitari verificano che la correzione sia nulla sopra la distanza minima e che non superi il limite configurato per piede. Non dimostrano da soli la qualità visiva del movimento in scena.
