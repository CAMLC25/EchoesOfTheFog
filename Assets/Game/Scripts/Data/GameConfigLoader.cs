using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace EchoesOfTheFog.Data
{
    public static class GameConfigLoader
    {
        public const string Header = "Id,Health,MoveSpeed,Damage,DetectionRadius,AttackRange,AttackCooldown,SearchDuration";

        // StreamingAssets is a regular directory on the project's Windows target.
        // Call explicitly at bootstrap; this class does not auto-load or silently fall back.
        public static IReadOnlyDictionary<string, EnemyConfig> LoadDefault()
            => Load(Path.Combine(Application.streamingAssetsPath, "GameConfig.csv"));

        public static IReadOnlyDictionary<string, EnemyConfig> Load(string path)
        {
            using (var reader = new StreamReader(path))
                return Parse(reader);
        }

        // Fixed, unquoted CSV schema. Decimal separator is always '.', regardless of OS locale.
        // Caller owns the reader. Parsing is atomic: no partial configuration is returned.
        public static IReadOnlyDictionary<string, EnemyConfig> Parse(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            string header = reader.ReadLine();
            if (header == null || header.TrimStart('\uFEFF').Trim() != Header)
                throw new FormatException($"GameConfig.csv line 1: expected header '{Header}'.");

            var configs = new Dictionary<string, EnemyConfig>(StringComparer.OrdinalIgnoreCase);
            string line;
            int lineNumber = 1;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] cells = line.Split(',');
                if (cells.Length != 8 || line.IndexOf('"') >= 0)
                    throw Error(lineNumber, "expected 8 unquoted comma-separated fields.");

                string id = cells[0].Trim();
                if (string.IsNullOrEmpty(id) || configs.ContainsKey(id))
                    throw Error(lineNumber, "enemy Id is empty or duplicated.");

                var values = new float[7];
                for (int i = 0; i < values.Length; i++)
                {
                    if (!float.TryParse(cells[i + 1].Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out values[i]) ||
                        float.IsNaN(values[i]) || float.IsInfinity(values[i]) || values[i] <= 0f)
                        throw Error(lineNumber, $"column {i + 2} must be a finite positive number.");
                }

                if (values[4] > values[3])
                    throw Error(lineNumber, "AttackRange must not exceed DetectionRadius.");

                configs.Add(id, new EnemyConfig(id, values[0], values[1], values[2],
                    values[3], values[4], values[5], values[6]));
            }

            if (!configs.ContainsKey("Runner") || !configs.ContainsKey("Tanker"))
                throw Error(lineNumber, "both Runner and Tanker rows are required.");

            return new ReadOnlyDictionary<string, EnemyConfig>(configs);
        }

        private static FormatException Error(int line, string message)
            => new FormatException($"GameConfig.csv line {line}: {message}");
    }
}
