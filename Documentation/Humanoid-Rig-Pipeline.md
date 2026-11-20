# Pipeline di preparazione dei modelli umanoidi

## Modelli sorgente e conservazione

Il progetto Blendstra [UnderGrind — Humans Source Separation](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3) contiene le sorgenti maschile e femminile:

- `Human_Male`: altezza modellata 1,78 m; 260 vertici e 486 triangoli.
- `Human_Female`: altezza modellata 1,70 m; 284 vertici e 529 triangoli.

Le due mesh originali devono rimanere conservate. Le modifiche di rigging e le varianti sono derivate e non devono sostituire le sorgenti.

L'esportazione di verifica non copre la configurazione dei modificatori e le coordinate UV valutate. Questi aspetti non sono certificati dai conteggi geometrici.

## Stato del prototipo di rig

La sorgente corrente è la versione Blendstra [`version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6`](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3&version=version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6). L'ispezione registra 16 oggetti:

- Due armature: `UG_Male_Armature` e `UG_Female_Armature`.
- Sei mesh derivate per personaggio: corpo, testa, braccia e mani dedicate alla prima e alla terza persona.
- Le due mesh sorgenti originali `Human_Male` e `Human_Female`, conservate e nascoste dal rendering.

I nomi delle mesh derivate corrispondono ai token di `PlayerRigSettings.asset`: `_Body`, `_Head`, `_TP_Arms`, `_TP_Hands`, `_FP_Arms` e `_FP_Hands`. La separazione testa/corpo è stata eseguita con un piano al 78% dell'altezza del bounding box; braccia e mani sono state estratte usando i gruppi di pesi delle ossa. Non si tratta di una separazione manuale dei poligoni.

Lo script che ha generato questa versione crea una gerarchia di 22 ossa per personaggio e tenta il weight painting automatico di Blender, con distribuzione per prossimità come fallback. L'ispezione sorgente non espone la configurazione completa dei modificatori né i pesi effettivi: la presenza degli oggetti non dimostra da sola che le deformazioni siano corrette.

`Tools/Blender/export-undergrind-humans.py` esegue i controlli direttamente in Blender prima dell'esportazione: ossa Humanoid richieste, sei mesh, parent corretti, modificatore Armature, gruppi di pesi collegati alle ossa deformanti e almeno un peso deformante valido per ogni vertice. Valida entrambi i personaggi prima di esportare e produce due FBX separati. I percorsi provengono da `PlayerRigSettings.asset` o, se l'asset non esiste ancora, dal template JSON.

La sorgente nativa della versione Blendstra è stata scaricata e versionata in `Assets/UnderGrind/Art/Source/humans.blend`. Il file corrisponde alla sorgente riggata verificata (SHA-256 `e74b80b77a8b7a9cfe607d2106de6a35bb65117548c91d587e6a77df0310e981`).

Le versioni `version_9333bec316691d50b66236e6ed6e172d9dbb5921f28563f3d3ae9b2b96ff0591`, `version_b8a60164bd0c3dbcb97cb79bf78d03069c370fc7c28abb093cd4025fbaf1a662` e il progetto separato `UnderGrind — Rigged Human Derivatives` sono iterazioni precedenti. La versione corrente con le 16 parti è la sorgente da esportare.

La sorgente corrente non contiene canali di animazione. Le clip locomotion vengono importate separatamente da PINOC. L'export FBX è stato eseguito e i due file sono versionati: `Human_Male_Rigged.fbx` (209.468 byte) e `Human_Female_Rigged.fbx` (212.300 byte). Il report `Tools/Blender/last-export-report.json` registra per entrambi 22 ossa e sei mesh non vuote (Body, Head, TP Arms/Hands, FP Arms/Hands). Questa verifica certifica l'export Blender, non l'importazione Unity o la qualità visiva delle deformazioni.

## Importazione in Unity e generazione dei prefab

Il rig Blender contiene entrambi i personaggi, ma Unity importa e genera un prefab separato per ciascuno. I due FBX devono quindi essere esportati separatamente e collocati nei percorsi configurati in `PlayerRigSettings.asset`:

- `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx`
- `Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx`

I percorsi sono modificabili nell'Inspector tramite `MaleSourceAssetPath` e `FemaleSourceAssetPath`. Ogni FBX deve contenere la mesh del personaggio, la relativa armatura configurata in `MaleArmatureRootName` o `FemaleArmatureRootName`, pesi di skinning verificati e un renderer della testa separato.

Quando entrambi gli FBX sono disponibili:

1. Verificare i percorsi nell'asset `Assets/UnderGrind/Settings/PlayerRigSettings.asset`.
2. Eseguire **UnderGrind → Player → Build Male and Female Prefabs From Configured FBX**.
3. Il builder importa ciascun FBX come Humanoid, crea il relativo Avatar, controlla che i due Avatar siano validi e distinti e genera i prefab nella cartella `Assets/UnderGrind/Generated/Prefabs`. Le opzioni di importazione di animazioni, materiali, camere, luci, ottimizzazione e curve di visibilità sono configurate in `PlayerRigSettings.asset`; il tipo Humanoid e la creazione dell'Avatar dal modello sono requisiti del rig.
4. Controllare che la testa sia separata e che i gruppi di renderer corrispondano ai token configurati.
5. Aprire i prefab e verificare le deformazioni prima di usarli nella scena.

Il builder rifiuta una sorgente se manca l'armatura configurata, l'Avatar Humanoid non è valido oppure la testa non è separata quando `RequireSeparateHead` è attivo. Non effettua tagli geometrici automatici. L'esportazione FBX è completata; il builder deve ancora essere eseguito in Unity per creare e validare i prefab.

`PlayerVisualRig` usa gruppi distinti di renderer:

- Testa: `HeadRendererSuffix`, disattivata in prima persona.
- Braccia e mani dedicate alla prima persona: `FirstPersonArmsToken` e `FirstPersonHandsToken`.
- Braccia e mani dedicate alla terza persona: `ThirdPersonArmsToken` e `ThirdPersonHandsToken`.

I gruppi dedicati alla prima e alla terza persona sono opzionali e configurabili. Se un gruppo non esiste, il sistema non inventa una mesh sostitutiva; resta visibile il corpo disponibile. Il renderer del corpo è identificato da `BodyRendererToken`. In prima persona il corpo viene nascosto quando la visuale è frontale e mostrato quando il giocatore guarda verso il basso: `FirstPersonBodyRevealPitch` definisce la soglia di comparsa e `FirstPersonBodyHidePitch` quella di scomparsa, con isteresi per evitare lampeggi vicino al limite. Entrambi i valori sono configurabili nell'Inspector.

La scena di test richiede entrambi i prefab riggati e verifica Animator, Avatar Humanoid, SkinnedMeshRenderer, testa, corpo e gruppi di arti. Se uno dei prefab manca, avvia la generazione dai due FBX configurati; se la validazione fallisce, interrompe la creazione della scena. Non usa mesh statiche come fallback.

La versione Blendstra più recente contiene quattro mesh separate per testa e corpo e le due armature hanno i nomi esatti richiesti. Questo supera il limite geometrico della prima revisione, ma non certifica ancora pesi, deformazioni, configurazione dei modificatori, UV o Avatar Humanoid. Il builder può quindi verificare gerarchia e renderer solo dopo l'esportazione e l'importazione dei due FBX; l'accettazione del rig resta subordinata ai test di deformazione in Unity.

## Verifica automatica dell'importazione FBX

`Assets/UnderGrind/Tests/Editor/HumanoidRigImportTests.cs` contiene test EditMode che caricano i due FBX effettivi dal progetto Unity. I controlli verificano:

- importazione Humanoid con Avatar generato dal modello e ottimizzazione GameObject disattivata;
- Avatar distinti, validi e Humanoid, con mapping delle ossa principali;
- sei `SkinnedMeshRenderer` per personaggio, mesh e bind pose presenti, root bone assegnato e riferimenti alle ossa risolti;
- riconoscimento delle sei parti da parte di `PlayerVisualRig`, con una testa, un corpo, due renderer FP e due TP.

Questi test verificano gli asset importati, non soltanto GameObject costruiti artificialmente. Devono essere eseguiti nel Unity Editor tramite Test Runner (EditMode). La workflow GitHub Actions attuale esegue i test standalone Diagnostics e non esegue i test Unity. Finché la nuova suite non viene eseguita in Unity, la validità degli Avatar e il binding dei renderer restano non verificati.

## Sorgenti delle animazioni

Sono stati individuati nella libreria PINOC otto movimenti:

- Standing Idle
- Walking
- Running
- Jump
- Crouching Idle
- Crouch Walk Forward

Le due famiglie di esportazione FBX hanno finalità diverse:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese le ossa facciali, senza mesh. È destinato ai personaggi PINOC con Splat Engine e rig Generic.
- `mixamo-fbx`: mesh X Bot e scheletro con nomenclatura Mixamo. È il formato candidato per il retargeting tramite Blender o altri strumenti DCC verso il rig UnderGrind.

X Bot è un modello di supporto per il retargeting e non deve sostituire il corpo del personaggio UnderGrind. Gli otto movimenti sono stati aggiunti alla libreria PINOC dell'utente. Gli asset selezionati sono:

| Movimento | Asset ID |
|---|---|
| Standing Idle | `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b` |
| Walking | `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060` |
| Running | `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a` |
| Jump | `08872e23-d695-493d-8c03-cb595cd55e9c` |
| Crouching Idle | `7cd56394-0b47-4fa7-b951-6658d5ea3da6` |
| Crouch Walk Forward | `e4f5b9e6-381c-4227-88d4-94d6a0615df8` |
| Fall down | `2e513074-b11f-4108-9ba7-80f2995ecba8` |
| Hard Landing | `241c8c51-4bfb-4f0c-ad0b-f9a7c50f8ee7` |

Per il rig Humanoid UnderGrind va usato l'export `mixamo-fbx` di ciascun asset: contiene X Bot e lo scheletro Mixamo ed è il formato previsto per il retargeting. Il formato predefinito restituito da `take_library_motions` è invece un GLB con solo lo scheletro MetaHuman a 441 ossa, senza mesh; è destinato al workflow Splat e non va assegnato direttamente al controller Humanoid.

Gli otto asset PINOC sono stati recuperati nuovamente e risultano disponibili nei formati `mixamo-fbx` e `mixamo-glb`; i link di download sono temporanei. I file FBX non sono ancora importati nel repository né trasferiti sul rig definitivo. Il builder Unity li riconosce se importati in `Assets/UnderGrind/Animations`. Prima di distribuire o includere gli export nel prodotto occorre verificare le condizioni d'uso applicabili.

Dopo l'importazione delle clip nella cartella `Assets/UnderGrind/Animations`, **UnderGrind → Player → Refresh Clips and Build Animator Controller** aggiorna i riferimenti e genera il controller usando `PlayerAnimationSettings.asset`. Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk sono richieste; Fall e Land sono opzionali.

## Criteri di completamento

- [ ] Mesh sorgenti maschile e femminile preservate e confrontate con i derivati.
- [ ] Isole geometriche disconnesse separate e ispezionate.
- [ ] Testa separata in un renderer indipendente, verificata da più angolazioni.
- [ ] Posizione delle ossa verificata su pose di riferimento.
- [ ] Pesi verificati con deformazioni di spalle, gomiti, polsi, anche, ginocchia e caviglie.
- [x] Export FBX maschile e femminile generati e versionati; report Blender con 22 ossa e sei mesh ciascuno.
- [ ] Avatar Humanoid Unity valido per entrambi i personaggi.
- [ ] Prefab maschile e femminile generati e controllati.
- [ ] Mesh dedicate a braccia e mani in prima persona create e posate, se richieste dal modello finale.
- [ ] Animazioni PINOC in formato Mixamo trasferite sullo scheletro UnderGrind.
- [ ] Cicli di camminata e corsa controllati per scivolamento dei piedi e movimento della radice.
- [ ] Test PlayMode superati sul rig importato.
- [ ] Visuale in prima e terza persona controllata nella scena Unity.
