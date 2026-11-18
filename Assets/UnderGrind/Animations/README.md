# Sorgenti delle animazioni del Player

Questa cartella contiene i file sorgente delle animazioni importate e i relativi file `.meta` generati da Unity.

## Nomi delle clip riconosciuti

Il generatore della scena di test effettua una ricerca senza distinzione tra maiuscole e minuscole. I nomi previsti sono:

- Idle / Standing Idle
- Walk / Walking
- Run / Running
- Jump
- Crouch Idle / Crouching Idle
- Crouch Walk

## Formati di esportazione PINOC

PINOC mette a disposizione due famiglie di file FBX, che non devono essere confuse:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese quelle facciali, senza mesh. È destinato ai personaggi PINOC utilizzati con Splat Engine in Unity tramite `com.viggle.splat-engine` dalla versione 0.2.2. L'importazione richiede il rig Generic e l'Avatar Definition impostata su No Avatar.
- `mixamo-fbx`: mesh X Bot e scheletro con nomi Mixamo. È destinato agli strumenti DCC e al retargeting su altri scheletri umanoidi. È il formato candidato per trasferire le animazioni sul corpo UnderGrind.

La mesh X Bot serve come sorgente per il retargeting: non è il modello del giocatore e non deve sostituirlo.

## Stato dell'importazione

I sei movimenti selezionati sono disponibili nella libreria PINOC, ma nessuna delle due famiglie di export è ancora importata in questa cartella o trasferita sul rig UnderGrind.

Il generatore della scena cerca attualmente le clip incorporate in `Assets/UnderGrind/Art/Source/humans.blend`. I file importati in questa cartella non vengono ancora individuati automaticamente dal generatore.

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

Prima di distribuire o includere nel prodotto gli export scaricati, verificare le condizioni d'uso applicabili del fornitore.
