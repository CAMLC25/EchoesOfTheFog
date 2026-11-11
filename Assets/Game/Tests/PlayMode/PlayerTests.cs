#if UNITY_EDITOR
using System.Collections;
using System.IO;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoesOfTheFog.Tests
{
    public sealed class PlayerPhysicsTests : InputTestFixture
    {
        private GameObject arena, actor, cameraObject;
        private Keyboard keyboard;
        private Mouse mouse;
        private Rigidbody body;
        private PlayerInputReader input;
        private PlayerCameraRig rig;

        public IEnumerator CreateArena()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            arena = new GameObject("Player physics test arena");
            Box("Floor", new Vector3(300, -0.5f, 0), new Vector3(40, 1, 40));
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/PlayerCapsule.prefab"));
            body = actor.GetComponent<Rigidbody>(); input = actor.GetComponent<PlayerInputReader>(); rig = actor.GetComponent<PlayerCameraRig>();
            body.position = new Vector3(300, 0.04f, 0);
            body.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            cameraObject = new GameObject("Physics test output camera", typeof(Camera), typeof(CinemachineBrain));
            cameraObject.GetComponent<CinemachineBrain>().UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            Time.timeScale = 3;
            yield return new WaitForSeconds(0.3f);
            input.SetCursorCaptured(true); rig.SetViewAngles(0, 12);
            Assert.That(body.position.x, Is.EqualTo(300).Within(0.05f), "Arena Rigidbody spawn was not synchronized.");
            Assert.That(body.position.y, Is.InRange(-0.05f, 0.1f), "Arena floor is not supporting the player.");
        }

        [UnityTearDown]
        public IEnumerator DestroyArena()
        {
            Object.Destroy(actor); Object.Destroy(arena); Object.Destroy(cameraObject);
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            yield return null;
        }

        private GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name;
            item.transform.SetParent(arena.transform); item.transform.position = position; item.transform.localScale = scale;
            Physics.SyncTransforms();
            return item;
        }

        [UnityTest]
        public IEnumerator DiagonalMovementIsNormalizedAndGrounded()
        {
            yield return CreateArena();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return new WaitForSeconds(0.8f);
            float speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            Assert.That(speed, Is.EqualTo(actor.GetComponent<PlayerMotor>().WalkSpeed).Within(0.15f));
            Assert.That(body.linearVelocity.x, Is.GreaterThan(1.5f));
            Assert.That(body.linearVelocity.z, Is.GreaterThan(1.5f));
            Assert.That(body.position.y, Is.InRange(-0.05f, 0.1f));
        }

        [UnityTest]
        public IEnumerator SprintConsumesStaminaThenRecoversWhileIdle()
        {
            yield return CreateArena();
            var stamina = actor.GetComponent<PlayerStamina>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(1.2f);
            Assert.That(body.linearVelocity.z, Is.GreaterThan(4.5f), $"position={body.position}, velocity={body.linearVelocity}, input={input.Move}, captured={input.CursorCaptured}, grounded={actor.GetComponent<PlayerMotor>().IsGrounded}, stamina={stamina.Current}");
            Assert.That(stamina.Current, Is.LessThan(stamina.Maximum - 10));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.3f);
            float depleted = stamina.Current;
            yield return new WaitForSeconds(2.2f);
            Assert.That(stamina.Current, Is.GreaterThan(depleted));
        }

        [UnityTest]
        public IEnumerator HoldingSprintWithoutMovingDoesNotDrain()
        {
            yield return CreateArena();
            Press(keyboard.leftShiftKey);
            yield return new WaitForSeconds(0.7f);
            Assert.That(actor.GetComponent<PlayerStamina>().Current, Is.EqualTo(100).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator WallStopsMovementWithoutDrainingStaminaForever()
        {
            yield return CreateArena();
            Box("Blocking wall", new Vector3(300, 1.5f, 2), new Vector3(6, 3, 0.4f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(1.4f);
            Assert.That(body.position.z, Is.LessThan(1.6f));
            Assert.That(body.position.z, Is.GreaterThan(1.1f));
            float stamina = actor.GetComponent<PlayerStamina>().Current;
            yield return new WaitForSeconds(1.5f);
            Assert.That(actor.GetComponent<PlayerStamina>().Current, Is.GreaterThanOrEqualTo(stamina - 0.5f));
        }

        [UnityTest]
        public IEnumerator LowStepIsClimbable()
        {
            yield return CreateArena();
            Box("Low step", new Vector3(300, 0.15f, 2.5f), new Vector3(5, 0.3f, 3));
            Press(keyboard.wKey);
            yield return new WaitForSeconds(1.15f);
            Assert.That(body.position.z, Is.GreaterThan(1.5f), $"Player stuck at low step: {body.position}");
            Assert.That(body.position.y, Is.GreaterThan(0.2f));
        }

        [UnityTest]
        public IEnumerator AimSlowsMovementAndChangesLens()
        {
            yield return CreateArena();
            Press(keyboard.wKey); Press(mouse.rightButton);
            yield return new WaitForSeconds(0.8f);
            Assert.That(body.linearVelocity.z, Is.InRange(1.6f, 2f));
            Assert.That(actor.GetComponentInChildren<CinemachineCamera>().Lens.FieldOfView, Is.InRange(43.5f, 45));
        }

        [UnityTest]
        public IEnumerator DeathStopsLocomotionAndReleasesCursor()
        {
            yield return CreateArena();
            Press(keyboard.wKey); yield return new WaitForSeconds(0.4f);
            actor.GetComponent<Health>().TakeDamage(100);
            Vector3 position = body.position;
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.ProjectOnPlane(body.position - position, Vector3.up).magnitude, Is.LessThan(0.1f));
            Assert.That(input.CursorCaptured, Is.False);
        }

        [UnityTest]
        public IEnumerator CursorReleaseBlocksInputAndClickRecaptures()
        {
            yield return CreateArena();
            Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey);
            Assert.That(input.CursorCaptured, Is.False);
            Press(keyboard.wKey); yield return new WaitForSeconds(0.3f);
            Assert.That(body.linearVelocity.z, Is.EqualTo(0).Within(0.05f));
            Press(mouse.leftButton); yield return null;
            Assert.That(input.CursorCaptured, Is.True);
        }

        [UnityTest]
        public IEnumerator SpaceJumpsAndHoldingItDoesNotBounceAfterLanding()
        {
            yield return CreateArena();
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.22f);
            Assert.That(body.position.y, Is.GreaterThan(0.5f));
            Assert.That(actor.GetComponent<PlayerMotor>().IsGrounded, Is.False);
            yield return new WaitForSeconds(1.2f);
            Assert.That(body.position.y, Is.InRange(-0.05f, 0.1f));
            Assert.That(actor.GetComponent<PlayerMotor>().IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator PressingSpaceInAirDoesNotDoubleJumpOrQueueALandingJump()
        {
            yield return CreateArena();
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.15f);
            Release(keyboard.spaceKey);
            yield return null;
            float before = body.linearVelocity.y;
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.08f);
            Assert.That(body.linearVelocity.y, Is.LessThan(before));
            yield return new WaitForSeconds(1.1f);
            Assert.That(body.position.y, Is.InRange(-0.05f, 0.1f));
        }

        [UnityTest]
        public IEnumerator JumpIsIgnoredWhenCursorIsReleased()
        {
            yield return CreateArena();
            input.SetCursorCaptured(false);
            Press(keyboard.spaceKey);
            yield return new WaitForSeconds(0.2f);
            Assert.That(body.position.y, Is.InRange(-0.05f, 0.1f));
            input.SetCursorCaptured(true);
            yield return new WaitForSeconds(0.2f);
            Assert.That(body.position.y, Is.InRange(-0.05f, 0.1f));
        }

        [UnityTest]
        public IEnumerator CameraPullsForwardWhenWallBlocksItsBoom()
        {
            yield return CreateArena();
            Box("Camera blocking wall", new Vector3(300, 2, -1.6f), new Vector3(7, 4, 0.3f));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.8f);
            Assert.That(cameraObject.transform.position.z, Is.GreaterThan(-1.5f));
            rig.SetViewAngles(3600, 200);
            Assert.That(rig.Pitch, Is.EqualTo(65));
        }
    }

    public sealed class MapPlayerWalkthroughTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator PlayerWalksFromForestAcrossTimberDeckToVillage()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/AssetValidation.unity", new LoadSceneParameters(LoadSceneMode.Single));
            // The user hid the navigation object while reviewing the map. Activate it only
            // in this Play Mode instance to obtain a driving path; do not save the scene.
            foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.name == "Navigation - walkable visual slice") item.gameObject.SetActive(true);
            yield return null;
            var actor = Object.FindFirstObjectByType<PlayerMotor>();
            Assert.That(actor, Is.Not.Null);
            var body = actor.GetComponent<Rigidbody>(); var rig = actor.GetComponent<PlayerCameraRig>();
            actor.GetComponent<PlayerInputReader>().SetCursorCaptured(true);
            yield return new WaitForSeconds(0.25f);
            Capture("01_Player_ForestStart");
            var destination = GameObject.Find("VillageEntrance").transform.position + Vector3.forward * 4;
            Assert.That(NavMesh.SamplePosition(destination, out var end, 3, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(body.position, out var start, 3, NavMesh.AllAreas), Is.True);
            var path = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            // Drive over the authored deck, not a potentially shorter NavMesh route through the creek.
            Vector3 crossing = GameObject.Find("Crossing").transform.position;
            Vector3[] checkpoints = { crossing + Vector3.back * 5, crossing, crossing + Vector3.forward * 5, destination };
            Time.timeScale = 3;
            Press(keyboard.wKey);
            float timeout = Time.time + 65;
            int corner = 0;
            bool crossedDeck = false;
            bool capturedCrossing = false, capturedVillage = false;
            while (Time.time < timeout && corner < checkpoints.Length)
            {
                Vector3 delta = checkpoints[corner] - body.position; delta.y = 0;
                if (corner == 1 && Mathf.Abs(body.position.z - crossing.z) < 0.6f)
                    Assert.That(body.position.y, Is.GreaterThan(0.12f), "Player crossed through the creek rather than on the deck.");
                if (delta.magnitude < 0.65f) { corner++; continue; }
                rig.SetViewAngles(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 12);
                if (body.position.z > 19) crossedDeck = true;
                if (!capturedCrossing && body.position.z > 7) { Capture("02_Player_Crossing"); capturedCrossing = true; }
                if (!capturedVillage && body.position.z > 34) { Capture("03_Player_VillageApproach"); capturedVillage = true; }
                Assert.That(actor.GetComponent<Health>().IsDead, Is.False, "Player fell or died on the route.");
                yield return null;
            }
            Release(keyboard.wKey);
            Capture("04_WalkthroughEnd");
            string report = $"Position={body.position}; checkpoint={corner}/{checkpoints.Length}; input={actor.GetComponent<PlayerInputReader>().Move}; captured={actor.GetComponent<PlayerInputReader>().CursorCaptured}; grounded={actor.IsGrounded}";
            if (corner < checkpoints.Length)
            {
                Vector3 direction = checkpoints[corner] - body.position; direction.y = 0;
                if (Physics.CapsuleCast(body.position + Vector3.up * 0.3f, body.position + Vector3.up * 1.5f, 0.28f, direction.normalized,
                    out var blocking, 1.5f, ~(1 << actor.gameObject.layer), QueryTriggerInteraction.Ignore)) report += $"; blocker={blocking.collider.name}, at={blocking.point}";
            }
            File.WriteAllText("Logs/Phase2/walkthrough.txt", report);
            Assert.That(crossedDeck, Is.True, "Player did not cross the timber deck. " + report);
            Assert.That(corner, Is.EqualTo(checkpoints.Length), $"Player stuck at {body.position}; next checkpoint {corner}.");
            Time.timeScale = 1;
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var map = SceneManager.GetSceneByPath("Assets/Game/Scenes/AssetValidation.unity");
            if (map.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("AfterPlayerMapWalkthrough"));
                yield return SceneManager.UnloadSceneAsync(map);
            }
            yield return null;
        }

        private static void Capture(string name)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Phase2");
                File.WriteAllBytes("Logs/Phase2/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previous;
                target.Release(); Object.Destroy(target); Object.Destroy(pixels);
            }
        }
    }
}
#endif
