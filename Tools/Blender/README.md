# Esportazione FBX dei personaggi UnderGrind

## Sorgente attesa

Il file Blender deve essere salvato in:

`Assets/UnderGrind/Art/Source/humans.blend`

La scena deve contenere due rig distinti:

- `UG_Male_Armature`
- `UG_Female_Armature`

Ogni rig deve avere sei mesh figlie:

- `Human_<Character>_Body`
- `Human_<Character>_Head`
- `Human_<Character>_TP_Arms`
- `Human_<Character>_TP_Hands`
- `Human_<Character>_FP_Arms`
- `Human_<Character>_FP_Hands`

Il token `<Character>` è `Male` o `Female`.

## Esecuzione automatizzata (Windows)

Dalla root del repository:

```powershell
./Tools/Blender/run-export.ps1
```

Il runner individua Blender tramite il comando `blender` nel PATH oppure tramite la variabile d'ambiente `BLENDER_EXECUTABLE`. In alternativa è possibile specificare `-BlenderExecutable` e `-TimeoutSeconds`.

Il runner legge i percorsi FBX da `PlayerRigSettings.asset` (con fallback al JSON predefinito), avvia Blender in background, acquisisce separatamente stdout/stderr e verifica codice di uscita, report, presenza dei due personaggi, sei mesh per personaggio e dimensioni effettive dei file.

Per impostazione predefinita non sostituisce FBX già esistenti. Per autorizzare esplicitamente la sostituzione:

```powershell
./Tools/Blender/run-export.ps1 -Force
```

Il timeout predefinito è 300 secondi. Per un'installazione Blender non presente nel PATH:

```powershell
./Tools/Blender/run-export.ps1 -BlenderExecutable "C:\\Program Files\\Blender Foundation\\Blender 4.5\\blender.exe"
```

## Esecuzione manuale

1. Aprire in Blender il file `Assets/UnderGrind/Art/Source/humans.blend`.
2. Aprire `Tools/Blender/export-undergrind-humans.py` nell'editor di testo di Blender.
3. Eseguire lo script con **Run Script**.
4. Controllare il report stampato nella console e il file `Tools/Blender/last-export-report.json`.

Lo script individua la radice del repository risalendo dalla posizione del file `.blend`. Legge i percorsi di output da `Assets/UnderGrind/Settings/PlayerRigSettings.asset`; se l'asset non è ancora stato creato, usa `Assets/UnderGrind/Settings/Defaults/PlayerRigSettings.json`.

Gli export vengono scritti nei percorsi configurati, inizialmente:

- `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx`
- `Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx`

## Controlli eseguiti prima dell'export

Per entrambi i personaggi lo script verifica:

- presenza dell'armatura e delle ossa Humanoid richieste;
- presenza di tutte le sei mesh nominate;
- parent diretto delle mesh all'armatura corretta;
- modificatore Armature collegato all'armatura corretta;
- gruppi di vertici associati alle ossa deformanti;
- almeno un peso deformante valido per ogni vertice;
- mesh non vuote;
- percorsi di output maschile e femminile distinti.

La validazione di entrambi i personaggi termina prima di iniziare gli export. Se un controllo fallisce, lo script interrompe il processo e non esporta un personaggio soltanto.

Lo script non taglia, non rigga, non modifica i pesi e non cambia la geometria: esporta soltanto le parti già presenti e collegate al rispettivo rig. Il modello deve quindi essere preparato e verificato prima dell'esecuzione.

## Importazione in Unity

Dopo l'export, Unity importa i due FBX. Eseguire:

**UnderGrind → Player → Build Male and Female Prefabs From Configured FBX**

Il builder Unity configura il rig Humanoid, genera gli Avatar e crea i prefab separati. La validità dell'Avatar e la presenza della testa separata vengono controllate durante la generazione.

Le animazioni non sono incluse in questi FBX. Le clip locomotion vengono importate separatamente nella cartella `Assets/UnderGrind/Animations` e configurate tramite `PlayerAnimationSettings.asset`.
