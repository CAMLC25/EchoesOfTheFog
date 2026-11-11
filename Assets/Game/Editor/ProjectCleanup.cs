using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EchoesOfTheFog.Editor
{
    public static class ProjectCleanup
    {
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity before batch cleanup.");
            // A deliberately small allowlist: no source pack, runtime asset, animation or test is inferred to be disposable.
            var candidates = new[]
            {
                "Assets/Scenes/SampleScene.unity",
                "Assets/Game/Prefabs/Player/Archive/Player_BeforeElena.prefab",
                "Assets/Game/Prefabs/Player/Archive/Player_BeforeRifleAnimations.prefab",
                "Assets/Game/Scenes/Archive/AssetValidation_BeforePlayer.unity"
            }.Where(File.Exists).ToArray();
            var allPaths = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/") && File.Exists(path)).ToArray();
            var candidateSet = new HashSet<string>(candidates);
            foreach (string path in candidates) VerifyPath(path);
            foreach (string owner in allPaths.Where(path => !candidateSet.Contains(path)))
            {
                var references = AssetDatabase.GetDependencies(owner, false).Where(candidateSet.Contains).ToArray();
                if (references.Length > 0) throw new InvalidOperationException($"Keep cleanup assets referenced by {owner}: {string.Join(", ", references)}");
            }
            foreach (var scene in EditorBuildSettings.scenes)
                if (candidateSet.Contains(scene.path)) throw new InvalidOperationException("Cleanup scene is listed in Build Settings: " + scene.path);
            var report = new StringBuilder("# Project cleanup\n\nRemoved obsolete assets from Unity's Assets tree after dependency checks. Recovery copies (including .meta) are local in `.utmp/Backups/Cleanup20261111/`; this directory is ignored by Git.\n\n");
            foreach (string path in candidates)
            {
                string backup = ProjectBackups.PreserveAsset(path, "Cleanup20261111");
                if (backup == null) throw new IOException("No recovery copy for " + path);
                if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not remove " + path);
                report.AppendLine("- Removed `" + path + "`.");
            }
            foreach (string path in new[] { "Assets/Scenes", "Assets/Game/Prefabs/Player/Archive", "Assets/Game/Scenes/Archive" })
            {
                VerifyPath(path);
                if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                {
                    if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not remove empty folder " + path);
                    report.AppendLine("- Removed empty folder `" + path + "`.");
                }
            }
            report.AppendLine("\nKept source Flooded Grounds assets, Terrain auto-upgrade layers, alternate weapons/animations, test scenes, tests and setup tools. No evidence establishes that these will be unnecessary in later phases. Setup tools now store backups outside Assets. Existing documentation references to Assets/.../Archive are historical; recovery files now live under the location above.\n");
            // Preserve the first manifest when rerunning an already-completed cleanup.
            if (candidates.Length > 0 || !File.Exists("Docs/Project-Cleanup.md")) File.WriteAllText("Docs/Project-Cleanup.md", report.ToString());
            AssetDatabase.SaveAssets();
        }

        private static void VerifyPath(string path)
        {
            string absolute = Path.GetFullPath(path);
            string assetRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cleanup target is outside this project's Assets folder: " + absolute);
        }
    }
}
