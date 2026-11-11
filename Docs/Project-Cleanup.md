# Project cleanup

Removed obsolete assets from Unity's Assets tree after dependency checks. Recovery copies (including .meta) are local in `.utmp/Backups/Cleanup20261111/`; this directory is ignored by Git.

- Removed `Assets/Scenes/SampleScene.unity`.
- Removed `Assets/Game/Prefabs/Player/Archive/Player_BeforeElena.prefab`.
- Removed `Assets/Game/Prefabs/Player/Archive/Player_BeforeRifleAnimations.prefab`.
- Removed `Assets/Game/Scenes/Archive/AssetValidation_BeforePlayer.unity`.
- Removed empty folder `Assets/Scenes`.
- Removed empty folder `Assets/Game/Prefabs/Player/Archive`.
- Removed empty folder `Assets/Game/Scenes/Archive`.

Kept source Flooded Grounds assets, Terrain auto-upgrade layers, alternate weapons/animations, test scenes, tests and setup tools. No evidence establishes that these will be unnecessary in later phases. Setup tools now store backups outside Assets. Existing documentation references to Assets/.../Archive are historical; recovery files now live under the location above.
