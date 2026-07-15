# Voidovia

Gritty low-fantasy warband / dynasty game built as a **standalone single-player Unity client**. There is no backend, database, Docker, or web service — the "application" is the Unity game itself (open `Assets/_Project/Scenes/WorldMap.unity` and press Play). See `README.md` for the gameplay overview and `Docs/DESIGN_LOCKS.txt` for locked design decisions.

## Cursor Cloud specific instructions

Environment facts that are baked into the VM snapshot (do not re-install these in the update script):

- **Unity Editor 2022.3.20f1** (Linux) is installed at `/opt/unity/2022.3.20f1/Editor/Unity`. Verify with `/opt/unity/2022.3.20f1/Editor/Unity --version` (prints `2022.3.20f1`). The official editor changeset for this version is `61c2feb0970d` (note: `ProjectSettings/ProjectVersion.txt` records a different, non-matching revision hash — the editor only keys off the `2022.3.20f1` version string, so this is harmless; do not "fix" it as part of environment setup).
- Unity's Linux runtime libraries plus **Xvfb** are installed. Unity is a GUI app, so any editor invocation needs a display: `Xvfb :99 -screen 0 1280x800x24 &` then `export DISPLAY=:99` (or wrap the command with `xvfb-run`).

### Offline validation (no license needed) — fast smoke check

`python3 Docs/validate_prototype.py` validates the actual game content and wiring without launching Unity: JSON integrity, world-map connectivity (BFS from `greyledger`), required troops, the boss battle card, scene entry wiring (`AppRoot` / `WorldMapEntry`), and presence of core C# types. Exit code `0` means the prototype is play-ready. This is the recommended first check after any content/script change; it uses only the Python 3 standard library.

### Unity license is required to compile / run / build (BLOCKER)

Opening the project, compiling C#, entering Play mode, and making builds all require an activated Unity license. Without one the editor exits immediately with `No valid Unity Editor license found. Please activate your license.` (confirmed via batchmode).

To activate, provide credentials as secrets, then activate once (the license persists under `~/.local/share/unity3d/`):

- **Plus/Pro (serial):** set `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_SERIAL`, then:
  `/opt/unity/2022.3.20f1/Editor/Unity -batchmode -nographics -quit -logFile /dev/stdout -serial "$UNITY_SERIAL" -username "$UNITY_EMAIL" -password "$UNITY_PASSWORD"`
- **Personal (manual .ulf):** generate an activation file with `-createManualActivationFile` (works offline; produces `Unity_v2022.3.20f1.alf`), upload it at https://license.unity3d.com/manual to get a `.ulf`, then place that file at `~/.local/share/unity3d/Unity/Unity_lic.ulf` (e.g. via a `UNITY_LICENSE` secret holding the ulf contents).

### Running / building once licensed

The game has no automated test suite and no `-executeMethod` entry point; it is exercised interactively. Useful headless commands (all need `DISPLAY` set via Xvfb):

- Import + compile scripts (generates `Library/`): `Unity -batchmode -nographics -quit -projectPath . -logFile /dev/stdout`
- Linux player build (add a small build-script `-executeMethod` if you need CI builds; none exists yet).

Build scene order (`ProjectSettings/EditorBuildSettings.asset`): `Bootstrap` → `WorldMap` → `Battle`. `Bootstrap`/`WorldMapEntry` bootstrap `AppFlow`, which loads JSON from `Assets/StreamingAssets/Data/` and shows character creation → world map. Saves are written to `Application.persistentDataPath/voidovia_save.json`.
