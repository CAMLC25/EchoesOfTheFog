using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoesOfTheFog.Editor
{
    public static class VillageAssetSurvey
    {
        public static void Inspect()
        {
            Directory.CreateDirectory("Logs/Village"); var report = new StringBuilder();
            string[] names = { "Buildings/Cabins/Cabin1", "Buildings/Cabins/Cabin1_DM", "Buildings/GuardHouse/GuardHouse_A", "Buildings/Structures1/Struct_Kiosk_A", "Buildings/Churches/Church1_Mid_A", "Buildings/BrickHouse/BrickHouse_Mid_A", "Buildings/Structures1/Struct_WoodPath_A", "Buildings/Structures1/Struct_Fence1_Mid_A", "Buildings/Structures1/Struct_Fence1_Gate_A", "Props/Prop_Car_A", "Props/Prop_Boat_A", "Props/Prop_LargeTable_A" };
            foreach (string name in names)
            {
                var item = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/" + name + ".prefab"));
                item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); item.transform.localScale = Vector3.one;
                var renderers = item.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                report.AppendLine(name + " bounds=" + bounds + " colliders=" + item.GetComponentsInChildren<Collider>().Length);
                Object.DestroyImmediate(item);
            }
            File.WriteAllText("Logs/Village/asset-survey.txt", report.ToString());
        }
    }
}
