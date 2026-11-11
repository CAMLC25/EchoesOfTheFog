using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [RequireComponent(typeof(Health), typeof(PlayerStamina), typeof(PlayerInputReader))]
    public sealed class PlayerPrototypeHUD : MonoBehaviour
    {
        private Health health;
        private PlayerStamina stamina;
        private PlayerInputReader input;
        private GUIStyle text;

        private void Awake()
        {
            health = GetComponent<Health>(); stamina = GetComponent<PlayerStamina>(); input = GetComponent<PlayerInputReader>();
        }

        private void OnGUI()
        {
            if (text == null) text = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            GUI.Box(new Rect(16, 16, 355, 128), GUIContent.none);
            GUI.Label(new Rect(28, 24, 330, 24), $"HEALTH  {health.CurrentHealth:0} / {health.MaxHealth:0}", text);
            GUI.Label(new Rect(28, 48, 330, 24), $"STAMINA  {stamina.Current:0} / {stamina.Maximum:0}", text);
            GUI.Label(new Rect(28, 76, 330, 24), "WASD move  |  Shift run  |  Space jump", text);
            GUI.Label(new Rect(28, 100, 330, 24), "RMB aim  |  Esc releases mouse", text);
            if (!input.CursorCaptured || health.IsDead)
            {
                string message = health.IsDead ? "Player down — stop Play Mode to restart" : "Click Game View to control  |  Esc releases mouse";
                GUI.Box(new Rect(Screen.width / 2f - 225, Screen.height - 66, 450, 40), message);
            }
        }
    }
}
