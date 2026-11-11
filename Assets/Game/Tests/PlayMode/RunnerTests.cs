#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Player;
using NUnit.Framework;
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
    public sealed class RunnerTests : InputTestFixture
    {
        private static readonly Vector3 Offset = new Vector3(500, 0, 0);
        private GameObject arena, enemy, player;
        private Health target;
        private RunnerBrain brain;
        private NavMeshDataInstance nav;
        private Keyboard keyboard;
        private Mouse mouse;

        private IEnumerator CreateArena()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            arena = new GameObject("Runner test arena");
            Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(40, 0.5f, 48));
            Box("Cover wall", new Vector3(3, 1.5f, 0), new Vector3(1, 3, 12));
            Box("Cover crate", new Vector3(-9, 1, 0), new Vector3(4, 2, 2));
            nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/Game/Data/Navigation/RunnerTest_NavMesh.asset"), Offset, Quaternion.identity);
            var dummy = new GameObject("Player target"); dummy.transform.SetParent(arena.transform); dummy.transform.position = Offset + Vector3.back * 8;
            dummy.layer = LayerMask.NameToLayer("Player"); target = dummy.AddComponent<Health>();
            var shape = GameObject.CreatePrimitive(PrimitiveType.Capsule); shape.transform.SetParent(dummy.transform, false);
            shape.transform.localPosition = Vector3.up * 0.9f; shape.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f); shape.layer = dummy.layer;
            enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Runner.prefab"), Offset, Quaternion.identity, arena.transform);
            brain = enemy.GetComponent<RunnerBrain>(); brain.SetTarget(target);
            var sun = new GameObject("Runner preview sun").AddComponent<Light>(); sun.transform.SetParent(arena.transform);
            sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, -40, 0);
            Physics.SyncTransforms(); Time.timeScale = 3;
            yield return new WaitForSeconds(0.3f);
            Assert.That(brain.NavigationReady, Is.True, "Runner did not bind to the test NavMesh.");
            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(40), "Runner must use CSV health.");
        }
        private GameObject Box(string name, Vector3 local, Vector3 scale)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.SetParent(arena.transform);
            item.transform.position = Offset + local; item.transform.localScale = scale; return item;
        }
        private Transform Waypoint(Vector3 local)
        { var point = new GameObject("Patrol point").transform; point.SetParent(arena.transform); point.position = Offset + local; return point; }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(arena); Object.Destroy(player); Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            yield return null; if (nav.valid) nav.Remove();
        }

        [UnityTest]
        public IEnumerator PatrolWaitsAtWaypointsAndModelStaysWithAgent()
        {
            yield return CreateArena(); target.transform.position = Offset + new Vector3(0, 0, -23);
            brain.SetPatrolPoints(new[] { Waypoint(new Vector3(0, 0, 3)), Waypoint(new Vector3(2, 0, 3)) });
            bool waited = false; brain.StateChanged += state => { if (state == RunnerState.Idle) waited = true; };
            Capture("Idle");
            yield return new WaitForSeconds(1);
            Capture("Walk");
            yield return new WaitForSeconds(3.2f);
            Assert.That(Vector3.Distance(enemy.transform.position, Offset), Is.GreaterThan(2));
            Assert.That(waited, Is.True, "Patrol should pause at a reached waypoint.");
            var animator = enemy.GetComponentInChildren<Animator>();
            Assert.That(animator.isHuman, Is.True); Assert.That(animator.applyRootMotion, Is.False);
            Vector3 hips = enemy.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position);
            Assert.That(new Vector2(hips.x, hips.z).magnitude, Is.LessThan(0.45f));
            Assert.That(enemy.transform.position.y, Is.InRange(-0.05f, 0.12f));
        }

        [UnityTest]
        public IEnumerator WallsAndFieldOfViewBlockVisionButNoiseStartsSearchAndTimesOut()
        {
            yield return CreateArena();
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle), "A quiet target behind the Runner is outside its initial field of view.");
            target.transform.position = Offset + new Vector3(7, 0, 0); enemy.transform.rotation = Quaternion.Euler(0, 90, 0); Physics.SyncTransforms();
            yield return new WaitForSeconds(0.4f);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle), "The wall must occlude the target.");
            GameplayNoise.Emit(target.transform.position, 1, target); yield return null;
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle), "An out-of-range noise must not alert the Runner.");
            GameplayNoise.Emit(target.transform.position, 26, target);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Search));
            target.transform.position = Offset + new Vector3(20, 0, 22); Physics.SyncTransforms();
            yield return new WaitForSeconds(5.6f);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle), "Search should end after the configured timeout when the target cannot be found.");
            brain.SetPatrolPoints(new[] { Waypoint(new Vector3(0, 0, 3)) });
            yield return new WaitForSeconds(0.2f); Assert.That(brain.State, Is.EqualTo(RunnerState.Patrol));
        }

        [UnityTest]
        public IEnumerator ChasesAroundCoverAndAttacksWithWindupAndCooldown()
        {
            yield return CreateArena();
            target.transform.position = Offset + new Vector3(7, 0, 0); Physics.SyncTransforms();
            var damageTimes = new List<float>(); target.HealthChanged += (current, maximum) => { if (current < maximum) damageTimes.Add(Time.time); };
            GameplayNoise.Emit(target.transform.position, 26, target);
            float timeout = Time.time + 10; bool chased = false;
            while (Time.time < timeout && damageTimes.Count < 2)
            {
                chased |= brain.State == RunnerState.Chase;
                Vector3 local = enemy.transform.position - Offset;
                if (Mathf.Abs(local.z) < 5.8f)
                    Assert.That(local.x < 2.3f || local.x > 3.7f, Is.True, "Runner crossed the baked wall footprint.");
                yield return null;
            }
            Assert.That(chased, Is.True); Assert.That(damageTimes.Count, Is.EqualTo(2), "Runner failed to navigate around the wall and attack.");
            Assert.That(damageTimes[1] - damageTimes[0], Is.GreaterThanOrEqualTo(brain.Config.AttackCooldown - 0.08f));
            Assert.That(target.CurrentHealth, Is.EqualTo(80));
            Capture("Attack");
        }

        [UnityTest]
        public IEnumerator DodgingDuringWindupAvoidsDamageAndDeathCancelsAttack()
        {
            yield return CreateArena();
            target.transform.position = Offset + new Vector3(0, 0, 1.2f); Physics.SyncTransforms();
            float timeout = Time.time + 1;
            while (brain.State != RunnerState.Attack && Time.time < timeout) yield return null;
            Assert.That(brain.State, Is.EqualTo(RunnerState.Attack)); Assert.That(target.CurrentHealth, Is.EqualTo(100));
            target.transform.position = Offset + new Vector3(0, 0, 6); Physics.SyncTransforms();
            yield return new WaitForSeconds(1.05f);
            Assert.That(target.CurrentHealth, Is.EqualTo(100), "An attack should miss if the target leaves its reach before impact.");
            enemy.GetComponent<Health>().TakeDamage(40);
            yield return new WaitForSeconds(1.3f);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Dead)); Assert.That(target.CurrentHealth, Is.EqualTo(100));
            Assert.That(brain.NavigationReady, Is.False); Assert.That(enemy.GetComponent<CapsuleCollider>().enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator TwoPlayerShotsCauseHitReactionThenDeath()
        {
            yield return CreateArena();
            CreateRealPlayer();
            yield return new WaitForSeconds(0.25f); player.GetComponent<PlayerInputReader>().SetCursorCaptured(true);
            var health = enemy.GetComponent<Health>();
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(15)); Assert.That(brain.State, Is.EqualTo(RunnerState.HitReact));
            Capture("Hit");
            yield return new WaitForSeconds(0.25f);
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
            Assert.That(health.CurrentHealth, Is.Zero); Assert.That(brain.State, Is.EqualTo(RunnerState.Dead));
            Assert.That(player.GetComponent<HitscanRifle>().Loaded, Is.EqualTo(28));
            yield return new WaitForSeconds(3.1f);
            Assert.That(enemy.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dead"), Is.True);
            Capture("Death");
        }

        [UnityTest]
        public IEnumerator ActualSprintEmitsNoiseWhileQuietWalkingDoesNot()
        {
            yield return CreateArena(); CreateRealPlayer();
            yield return new WaitForSeconds(0.25f); player.GetComponent<PlayerInputReader>().SetCursorCaptured(true);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.6f);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle), "Quiet walking behind the Runner should not emit sprint noise.");
            bool heard = false; brain.StateChanged += state => { if (state == RunnerState.Search) heard = true; };
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return new WaitForSeconds(0.7f);
            Assert.That(heard, Is.True, "Actual sprint movement should alert the Runner through PlayerNoiseEmitter.");
        }

        [UnityTest]
        public IEnumerator CoverInsertedDuringWindupBlocksMeleeImpact()
        {
            yield return CreateArena();
            target.transform.position = Offset + new Vector3(0, 0, 1.2f); Physics.SyncTransforms();
            float timeout = Time.time + 1;
            while (brain.State != RunnerState.Attack && Time.time < timeout) yield return null;
            Assert.That(brain.State, Is.EqualTo(RunnerState.Attack));
            Box("Melee cover", new Vector3(0, 1.5f, 0.65f), new Vector3(4, 3, 0.15f)); Physics.SyncTransforms();
            yield return new WaitForSeconds(0.7f);
            Assert.That(target.CurrentHealth, Is.EqualTo(100), "Melee damage must recheck occlusion at impact, even if the target was visible at attack start.");
        }

        [UnityTest]
        public IEnumerator TargetDeathDuringAttackReturnsRunnerToIdle()
        {
            yield return CreateArena();
            target.transform.position = Offset + new Vector3(0, 0, 1.2f); Physics.SyncTransforms();
            float timeout = Time.time + 1;
            while (brain.State != RunnerState.Attack && Time.time < timeout) yield return null;
            Assert.That(brain.State, Is.EqualTo(RunnerState.Attack)); target.TakeDamage(100);
            yield return new WaitForSeconds(0.6f);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle)); Assert.That(brain.NavigationReady, Is.True);
        }

        [UnityTest]
        public IEnumerator MissingTargetAndOffNavMeshRemainIdleWithoutErrors()
        {
            yield return CreateArena();
            Object.Destroy(target.gameObject); yield return new WaitForSeconds(0.3f);
            Assert.That(brain.State, Is.EqualTo(RunnerState.Idle));
            enemy.GetComponent<NavMeshAgent>().enabled = false; enemy.transform.position = new Vector3(5000, 0, 0);
            yield return new WaitForSeconds(1.2f);
            Assert.That(brain.NavigationReady, Is.False); Assert.That(brain.State, Is.EqualTo(RunnerState.Idle));
            LogAssert.NoUnexpectedReceived();
        }

        private void CreateRealPlayer()
        {
            target.gameObject.SetActive(false);
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/PlayerCapsule.prefab"), Offset + new Vector3(0, 0.04f, -8), Quaternion.identity);
            player.GetComponent<Rigidbody>().position = Offset + new Vector3(0, 0.04f, -8);
            player.GetComponent<PlayerCameraRig>().SetViewAngles(0, 0); brain.SetTarget(player.GetComponent<Health>());
            var output = new GameObject("Main Camera", typeof(Camera)); output.transform.SetParent(arena.transform); output.tag = "MainCamera";
            output.transform.SetPositionAndRotation(Offset + new Vector3(0, 1.15f, -11), Quaternion.identity);
            Physics.SyncTransforms();
        }

        private void Capture(string name) => CapturePose(enemy, name);

        internal static void CapturePose(GameObject enemy, string name)
        {
            var item = new GameObject("Runner pose camera"); var camera = item.AddComponent<Camera>();
            camera.transform.position = enemy.transform.position + new Vector3(2.8f, 1.8f, 3.5f);
            if (name == "Attack") camera.transform.position = enemy.transform.position + new Vector3(4, 1.8f, -0.2f);
            camera.transform.LookAt(enemy.transform.position + Vector3.up * 0.95f); camera.fieldOfView = 34;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.22f, 0.25f, 0.28f);
            var rt = new RenderTexture(960, 720, 24); var pixels = new Texture2D(960, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); pixels.Apply(); Directory.CreateDirectory("Logs/Runner");
                File.WriteAllBytes("Logs/Runner/Runner_" + name + ".png", pixels.EncodeToPNG());
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; rt.Release(); Object.Destroy(rt); Object.Destroy(pixels); Object.Destroy(item); }
        }
    }

    public sealed class RunnerForestTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator ForestSceneHasFiveNavigatingRunnersTargetingTheSharedPlayer()
        {
            InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Game/Scenes/Main/Level01_Forest.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(0.6f);
            var runners = Object.FindObjectsByType<RunnerBrain>(FindObjectsSortMode.None);
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            Assert.That(runners.Length, Is.EqualTo(5)); Assert.That(player, Is.Not.Null);
            foreach (var runner in runners)
            {
                Assert.That(runner.NavigationReady, Is.True);
                Assert.That(runner.Target, Is.EqualTo(player.GetComponent<Health>()));
                Assert.That(runner.State, Is.EqualTo(RunnerState.Patrol).Or.EqualTo(RunnerState.Idle));
                Assert.That(NavMesh.SamplePosition(player.transform.position, out var playerFloor, 2, NavMesh.AllAreas), Is.True);
                var path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(runner.transform.position, playerFloor.position, NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), "Each Runner must be able to reach the player's route.");
            }
            RunnerTests.CapturePose(runners[0].gameObject, "Forest");
            Assert.That(player.GetComponent<Health>().CurrentHealth, Is.EqualTo(100), "The player should have a safe starting distance.");
        }

        [UnityTearDown]
        public IEnumerator CleanupScene()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var map = SceneManager.GetSceneByPath("Assets/Game/Scenes/Main/Level01_Forest.unity");
            if (map.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("AfterRunnerForestTest"));
                yield return SceneManager.UnloadSceneAsync(map);
            }
        }
    }
}
#endif
