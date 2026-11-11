using System;
using System.IO;

namespace EchoesOfTheFog.Editor
{
    // Local recovery copies belong outside Assets so Unity never imports duplicate scenes/prefabs.
    internal static class ProjectBackups
    {
        public static string PreserveAsset(string assetPath, string label)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) || assetPath.Contains("..") || label.Contains("/") || label.Contains("\\"))
                throw new ArgumentException("Expected a project asset path and a plain backup label.");
            if (!File.Exists(assetPath)) return null;
            string destination = ".utmp/Backups/" + label + "/" + assetPath;
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (!File.Exists(destination)) File.Copy(assetPath, destination);
            if (File.Exists(assetPath + ".meta") && !File.Exists(destination + ".meta")) File.Copy(assetPath + ".meta", destination + ".meta");
            return destination;
        }
    }
}
