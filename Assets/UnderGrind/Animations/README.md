# Sorgenti delle animazioni del Player

Questa cartella contiene i file sorgente delle animazioni importate e i relativi file `.meta` generati da Unity.

## Nomi delle clip riconosciuti

Il generatore della scena di test effettua una ricerca senza distinzione tra maiuscole e minuscole. I nomi previsti sono:

- Idle / Standing Idle
- Walk / Walking
- Run / Running
- Jump
- Fall / Fall down
- Land / Hard Landing
- Crouch Idle / Crouching Idle
- Crouch Walk

## Formati di esportazione PINOC

PINOC mette a disposizione due famiglie di file FBX, che non devono essere confuse:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese quelle facciali, senza mesh. È destinato ai personaggi PINOC utilizzati con Splat Engine in Unity tramite `com.viggle.splat-engine` dalla versione 0.2.2. L'importazione richiede il rig Generic e l'Avatar Definition impostata su No Avatar.
- `mixamo-fbx`: mesh X Bot e scheletro con nomi Mixamo. È destinato agli strumenti DCC e al retargeting su altri scheletri umanoidi. È il formato candidato per trasferire le animazioni sul corpo UnderGrind.

La mesh X Bot serve come sorgente per il retargeting: non è il modello del giocatore e non deve sostituirlo.

## Stato dell'importazione

Gli otto movimenti selezionati sono disponibili nella libreria PINOC, ma nessuna delle due famiglie di export è ancora importata in questa cartella o trasferita sul rig UnderGrind.

`PlayerAnimationControllerBuilder` individua i file FBX importati in questa cartella, imposta l'importazione Humanoid e associa le clip mancanti usando il nome della clip o del file. Per generare il controller completo sono necessarie tutte e otto le clip elencate sopra. Il loop di ogni clip è configurabile separatamente nell'asset `PlayerAnimationSettings.asset`; il generatore applica tali valori ai rispettivi `ModelImporterClipAnimation` prima di aggiornare i riferimenti.

`LocomotionTestSceneBuilder` non cerca clip dentro `humans.blend`: usa l'eventuale Avatar presente nel file soltanto come fallback per il modello statico. Il controller di animazione viene assegnato tramite `PlayerAnimationSettings.asset`. Il modello sorgente attuale non dispone di un Avatar Humanoid valido né di renderer separati per testa e arti FP.

## Provenienza dei movimenti selezionati

I seguenti asset della libreria PINOC sono stati selezionati e acquisiti senza consumo di crediti:

| Movimento | Identificativo asset |
|---|---|
| Standing Idle | `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b` |
| Walking | `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060` |
| Running | `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a` |
| Jump | `08872e23-d695-493d-8c03-cb595cd55e9c` |
| Crouching Idle | `7cd56394-0b47-4fa7-b951-6658d5ea3da6` |
| Crouch Walk Forward | `e4f5b9e6-381c-4227-88d4-94d6a0615df8` |
| Fall down | `2e513074-b11f-4108-9ba7-80f2995ecba8` |
| Hard Landing | `241c8c51-4bfb-4f0c-ad0b-f9a7c50f8ee7` |

Prima di distribuire o includere nel prodotto gli export scaricati, verificare le condizioni d'uso applicabili del fornitore.
