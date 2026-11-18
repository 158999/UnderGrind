# Pipeline di preparazione dei modelli umanoidi

## Modelli sorgente e conservazione

Il progetto Blendstra [UnderGrind — Humans Source Separation](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3) contiene le sorgenti maschile e femminile:

- `Human_Male`: altezza modellata 1,78 m; 260 vertici e 486 triangoli.
- `Human_Female`: altezza modellata 1,70 m; 284 vertici e 529 triangoli.

Le due mesh originali devono rimanere conservate. Le modifiche di rigging e le varianti sono derivate e non devono sostituire le sorgenti.

L'esportazione di verifica non copre la configurazione dei modificatori e le coordinate UV valutate. Questi aspetti non sono certificati dai conteggi geometrici.

## Stato del prototipo di rig

La versione salvata `version_9333bec316691d50b66236e6ed6e172d9dbb5921f28563f3d3ae9b2b96ff0591` contiene:

- Le mesh originali `Human_Male` e `Human_Female`, conservate e nascoste dal rendering.
- Copie `Human_Male_Body` e `Human_Female_Body`, con gli stessi conteggi di vertici e triangoli delle rispettive sorgenti.
- Le radici armature `UG_Male_Armature` e `UG_Female_Armature`.

Il codice che ha generato il prototipo colloca le ossa usando proporzioni ricavate dai bounding box e prova ad assegnare i pesi automatici di Blender, con una distribuzione per prossimità come fallback. Questi passaggi non sono ancora verificati con test di deformazione. L'esportazione nativa disponibile non espone il dettaglio delle ossa né la configurazione dei pesi. La versione risulta inoltre priva di canali di animazione.

Il prototipo non è quindi un rig Humanoid approvato per la produzione. In particolare, ogni personaggio ha ancora una sola mesh del corpo: non è presente un renderer separato per testa/viso. Non è possibile nascondere soltanto la testa in prima persona.

Il progetto separato [UnderGrind — Rigged Human Derivatives](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2493fca7-8542-4d61-8a81-12487e6a3999) contiene la versione iniziale `version_3f8ecc6facc3cfa2f4f9a2234af25ae75c6cedf4dd39d94edbf0c485066935c0` e la revisione aggiornata `version_761640485ef91a3c2d77dde4cba3ea1412e032e2aa753f08c99379afff77828b`, generata usando la regione del vertex group `Head` con soglia di peso 0,35.

L'ispezione della sorgente registra quattro mesh derivate distinte:
- `UG_Male_Head`: 67 vertici e 130 triangoli.
- `UG_Male_Body`: 202 vertici e 400 triangoli.
- `UG_Female_Head`: 30 vertici e 53 triangoli.
- `UG_Female_Body`: 266 vertici e 528 triangoli.

Le mesh originali restano nel progetto sorgente importato. Il risultato include copie delle armature e mantiene i riferimenti ai componenti sorgente. L'ispezione registra ancora controlli non supportati per configurazione dei modificatori e coordinate UV valutate; il GLB esportato dichiara zero animazioni e non contiene materiali assegnati. La separazione della testa è quindi stata eseguita geometricamente, ma non è una prova di correttezza dei pesi, della deformazione o della topologia dei bordi.

Nella prima esportazione le copie delle armature avevano ricevuto il suffisso automatico `.001`. La revisione Blendstra successiva è terminata: l'ispezione della versione `version_761640485ef91a3c2d77dde4cba3ea1412e032e2aa753f08c99379afff77828b` conferma i nomi esatti `UG_Male_Armature` e `UG_Female_Armature`, senza suffisso. Sono inoltre presenti le quattro mesh separate per testa e corpo. L'ispezione registra 13 oggetti, nessun materiale assegnato e zero canali di animazione. Restano non supportati i controlli su preservazione completa della sorgente, configurazione dei modificatori, animazioni, adattamento tecnico e permessi delle dipendenze. I nomi richiesti dal builder sono quindi corretti, ma non è ancora dimostrato un rig deformabile né un Avatar Humanoid valido. Il modello non è stato importato nel repository e non è accettato come prefab Humanoid di produzione.

La revisione precedente `version_b8a60164bd0c3dbcb97cb79bf78d03069c370fc7c28abb093cd4025fbaf1a662` aveva eseguito una separazione per sole isole geometriche disconnesse. Il risultato conteneva una sola mesh per personaggio: ciascun corpo era un'unica isola connessa e la testa non poteva essere separata con quell'operazione topologica. La successiva separazione per regione pesata descritta sopra è un'operazione distinta.

## Importazione in Unity e generazione dei prefab

Il rig Blender contiene entrambi i personaggi, ma Unity importa e genera un prefab separato per ciascuno. I due FBX devono quindi essere esportati separatamente e collocati nei percorsi configurati in `PlayerRigSettings.asset`:

- `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx`
- `Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx`

I percorsi sono modificabili nell'Inspector tramite `MaleSourceAssetPath` e `FemaleSourceAssetPath`. Ogni FBX deve contenere la mesh del personaggio, la relativa armatura configurata in `MaleArmatureRootName` o `FemaleArmatureRootName`, pesi di skinning verificati e un renderer della testa separato.

Quando entrambi gli FBX sono disponibili:

1. Verificare i percorsi nell'asset `Assets/UnderGrind/Settings/PlayerRigSettings.asset`.
2. Eseguire **UnderGrind → Player → Build Male and Female Prefabs From Configured FBX**.
3. Il builder importa ciascun FBX come Humanoid, crea il relativo Avatar, controlla che i due Avatar siano validi e distinti e genera i prefab nella cartella `Assets/UnderGrind/Generated/Prefabs`.
4. Controllare che la testa sia separata e che i gruppi di renderer corrispondano ai token configurati.
5. Aprire i prefab e verificare le deformazioni prima di usarli nella scena.

Il builder rifiuta una sorgente se manca l'armatura configurata, l'Avatar Humanoid non è valido oppure la testa non è separata quando `RequireSeparateHead` è attivo. Non effettua tagli geometrici automatici.

`PlayerVisualRig` usa gruppi distinti di renderer:

- Testa: `HeadRendererSuffix`, disattivata in prima persona.
- Braccia e mani dedicate alla prima persona: `FirstPersonArmsToken` e `FirstPersonHandsToken`.
- Braccia e mani dedicate alla terza persona: `ThirdPersonArmsToken` e `ThirdPersonHandsToken`.

I gruppi dedicati alla prima e alla terza persona sono opzionali e configurabili. Se un gruppo non esiste, il sistema non inventa una mesh sostitutiva; resta visibile il corpo disponibile. Il renderer del corpo è identificato da `BodyRendererToken`. In prima persona il corpo viene nascosto quando la visuale è frontale e mostrato quando il giocatore guarda verso il basso: `FirstPersonBodyRevealPitch` definisce la soglia di comparsa e `FirstPersonBodyHidePitch` quella di scomparsa, con isteresi per evitare lampeggi vicino al limite. Entrambi i valori sono configurabili nell'Inspector.

La scena di test usa i prefab riggati configurati quando esistono. Se il prefab maschile non è disponibile, può usare la mesh statica maschile come fallback visivo; in quel caso il personaggio si muove ma non dispone di un rig animabile.

La versione Blendstra più recente contiene quattro mesh separate per testa e corpo e le due armature hanno i nomi esatti richiesti. Questo supera il limite geometrico della prima revisione, ma non certifica ancora pesi, deformazioni, configurazione dei modificatori, UV o Avatar Humanoid. Il builder può quindi verificare gerarchia e renderer solo dopo l'esportazione e l'importazione dei due FBX; l'accettazione del rig resta subordinata ai test di deformazione in Unity.

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
- [ ] Avatar Humanoid Unity valido per entrambi i personaggi.
- [ ] Prefab maschile e femminile generati e controllati.
- [ ] Mesh dedicate a braccia e mani in prima persona create e posate, se richieste dal modello finale.
- [ ] Animazioni PINOC in formato Mixamo trasferite sullo scheletro UnderGrind.
- [ ] Cicli di camminata e corsa controllati per scivolamento dei piedi e movimento della radice.
- [ ] Test PlayMode superati sul rig importato.
- [ ] Visuale in prima e terza persona controllata nella scena Unity.
