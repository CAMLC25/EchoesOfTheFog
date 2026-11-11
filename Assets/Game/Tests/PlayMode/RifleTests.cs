#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace EchoesOfTheFog.Tests
{
    public sealed class RifleTests : InputTestFixture
    {
        private GameObject arena, actor;
        private Camera camera;
        private Keyboard keyboard;
        private Mouse mouse;
        private HitscanRifle rifle;
        private Health target;

        private IEnumerator CreateArena()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            arena = new GameObject("Rifle test arena");
            Box("Floor", new Vector3(300, -0.5f, 0), new Vector3(40, 1, 40));
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/PlayerCapsule.prefab"));
            actor.GetComponent<Rigidbody>().position = new Vector3(300, 0.04f, 0);
            actor.GetComponent<Rigidbody>().rotation = Quaternion.identity;
            actor.GetComponent<PlayerCameraRig>().SetViewAngles(0, 0);
            camera = new GameObject("Rifle test camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(arena.transform); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(300, 1.3f, -4); camera.transform.rotation = Quaternion.identity;
            target = Box("Damageable target", new Vector3(300, 1.5f, 10), new Vector3(4, 3, 1)).AddComponent<Health>();
            var sun = new GameObject("Preview sun").AddComponent<Light>(); sun.transform.SetParent(arena.transform);
            sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            Physics.SyncTransforms(); Time.timeScale = 3;
            yield return new WaitForSeconds(0.4f);
            actor.GetComponent<PlayerInputReader>().SetCursorCaptured(true);
            rifle = actor.GetComponent<HitscanRifle>();
        }

        private GameObject Box(string name, Vector3 position, Vector3 scale)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name;
            item.transform.SetParent(arena.transform); item.transform.position = position; item.transform.localScale = scale;
            return item;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(actor); Object.Destroy(arena); Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; yield return null;
        }

        [Test]
        public void AmmoReloadConservesRoundsAndStopsAtZero()
        {
            var ammo = new WeaponAmmo(30, 2);
            for (int i = 0; i < 5; i++) Assert.That(ammo.TryConsume(), Is.True);
            ammo.CompleteReload(); Assert.That(ammo.Loaded, Is.EqualTo(27)); Assert.That(ammo.Reserve, Is.Zero);
            Assert.That(ammo.CanReload, Is.False);
            for (int i = 0; i < 27; i++) Assert.That(ammo.TryConsume(), Is.True);
            Assert.That(ammo.TryConsume(), Is.False); ammo.CompleteReload(); Assert.That(ammo.Loaded, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HitscanDamagesOnceRespectsCoverAndReloadsOnlyAfterDelay()
        {
            yield return CreateArena();
            Assert.That(rifle.TryFire(), Is.True); Assert.That(target.CurrentHealth, Is.EqualTo(75));
            Assert.That(rifle.TryFire(), Is.False, "Fire rate must limit shots within the same frame.");
            yield return new WaitForSeconds(0.25f);
            var cover = Box("Cover", new Vector3(300, 1.5f, 4), new Vector3(4, 3, 0.3f)); Physics.SyncTransforms();
            Assert.That(rifle.TryFire(), Is.True); Assert.That(target.CurrentHealth, Is.EqualTo(75), "Cover must stop damage.");
            Assert.That(rifle.Loaded, Is.EqualTo(28)); Assert.That(rifle.TryStartReload(), Is.True);
            yield return new WaitForSeconds(0.3f);
            Assert.That(rifle.Loaded, Is.EqualTo(28)); Assert.That(rifle.TryFire(), Is.False);
            Assert.That(actor.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(1).IsName("Reload"), Is.True);
            Capture("Reload");
            yield return new WaitForSeconds(3.3f);
            Assert.That(rifle.IsReloading, Is.False); Assert.That(rifle.Loaded, Is.EqualTo(30)); Assert.That(rifle.Reserve, Is.EqualTo(88));
            Assert.That(rifle.TryStartReload(), Is.False, "A full magazine must not start a reload.");
            cover.SetActive(false);
        }

        [UnityTest]
        public IEnumerator RecaptureClickDoesNotFireButNextClickAndRDo()
        {
            yield return CreateArena();
            var input = actor.GetComponent<PlayerInputReader>(); input.SetCursorCaptured(false);
            Press(mouse.leftButton); yield return null; yield return null;
            Assert.That(input.CursorCaptured, Is.True); Assert.That(rifle.Loaded, Is.EqualTo(30));
            Release(mouse.leftButton); yield return null;
            Press(mouse.leftButton); yield return null; yield return null;
            Release(mouse.leftButton); yield return null;
            Assert.That(rifle.Loaded, Is.EqualTo(29)); Assert.That(target.CurrentHealth, Is.EqualTo(75));
            Press(keyboard.rKey); yield return null; yield return null;
            Assert.That(rifle.IsReloading, Is.True);
            actor.GetComponent<Health>().TakeDamage(100);
            yield return new WaitForSeconds(3.6f);
            Assert.That(rifle.IsReloading, Is.False); Assert.That(rifle.Loaded, Is.EqualTo(29));
            Assert.That(rifle.TryFire(), Is.False); Assert.That(rifle.TryStartReload(), Is.False);
            Assert.That(actor.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dead"), Is.True);
        }

        [UnityTest]
        public IEnumerator HipFireTurnsBodyAndBarrelWithCameraWithoutEnteringAimOrSprint()
        {
            yield return CreateArena();
            var rig = actor.GetComponent<PlayerCameraRig>();
            var weapon = actor.GetComponentInChildren<RifleView>();
            foreach (float yaw in new[] { 90f, 180f })
            {
                Quaternion facing = Quaternion.Euler(8, yaw, 0);
                Vector3 center = actor.transform.position + Vector3.up * 1.3f;
                rig.SetViewAngles(yaw, 8);
                camera.transform.SetPositionAndRotation(center - facing * Vector3.forward * 4, facing);
                target.transform.position = actor.transform.position + Vector3.up * 1.5f + Vector3.ProjectOnPlane(facing * Vector3.forward, Vector3.up).normalized * 6;
                target.ResetHealth(100); Physics.SyncTransforms();
                Press(mouse.leftButton);
                yield return new WaitForSeconds(0.45f);
                Assert.That(rig.IsAiming, Is.False);
                Assert.That(Quaternion.Angle(actor.GetComponent<Rigidbody>().rotation, Quaternion.Euler(0, yaw, 0)), Is.LessThan(4));
                Assert.That(Vector3.Dot(weapon.transform.forward, facing * Vector3.forward), Is.GreaterThan(0.98f));
                Assert.That(target.CurrentHealth, Is.LessThan(100), "Hip fire must hit along the camera direction.");
                Assert.That(actor.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>().Lens.FieldOfView, Is.EqualTo(58).Within(0.1f));
                Capture("HipFire" + yaw);
                Release(mouse.leftButton); yield return new WaitForSeconds(0.55f);
            }
            rig.SetViewAngles(270, 0);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.LeftShift)); Press(mouse.leftButton);
            yield return new WaitForSeconds(0.45f);
            Assert.That(actor.GetComponent<PlayerMotor>().IsSprinting, Is.False, "Firing must keep the strafe/fire pose rather than sprinting away from the reticle.");
            Assert.That(Quaternion.Angle(actor.GetComponent<Rigidbody>().rotation, Quaternion.Euler(0, 270, 0)), Is.LessThan(4));
            Release(mouse.leftButton); InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.8f);
            Quaternion settled = actor.GetComponent<Rigidbody>().rotation;
            rig.SetViewAngles(0, 0); yield return new WaitForSeconds(0.4f);
            Assert.That(Quaternion.Angle(actor.GetComponent<Rigidbody>().rotation, settled), Is.LessThan(0.2f), "Orbiting without aim/fire should remain independent of the body.");
        }

        [UnityTest]
        public IEnumerator BothJumpClipsFollowPhysicsWithoutMovingModelAwayFromCapsule()
        {
            yield return CreateArena();
            var weapon = actor.GetComponentInChildren<RifleView>();
            Assert.That(weapon.transform.Find("Gun31_Visual"), Is.Not.Null, "The shared weapon wrapper must use Gun_31.");
            yield return CheckJump("Rifle Jump", "RifleJump");
            rifle.enabled = false; weapon.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            yield return CheckJump("Jump", "UnarmedJump");
        }

        private IEnumerator CheckJump(string clipName, string capture)
        {
            var animator = actor.GetComponentInChildren<Animator>();
            var body = actor.GetComponent<Rigidbody>();
            float startY = body.position.y, peak = startY;
            bool sawClip = false, captured = false;
            Press(keyboard.spaceKey); yield return null; Release(keyboard.spaceKey);
            float finish = Time.time + 1.5f;
            while (Time.time < finish)
            {
                peak = Mathf.Max(peak, body.position.y);
                if (!actor.GetComponent<PlayerMotor>().IsGrounded && body.position.y > startY + 0.35f)
                {
                    sawClip |= animator.GetCurrentAnimatorClipInfo(0).Any(info => info.clip.name == clipName && info.weight > 0.9f);
                    Vector3 offset = actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position);
                    Assert.That(new Vector2(offset.x, offset.z).magnitude, Is.LessThan(0.5f), "Animation must not translate away from the physics body.");
                    float feet = Mathf.Min(actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position).y,
                        actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot).position).y);
                    Assert.That(feet, Is.GreaterThan(-0.3f), $"Jump feet must not sink beneath the physics capsule: hips={offset}, phase={animator.GetFloat("JumpPhase")}, clip={clipName}");
                    Assert.That(offset.y, Is.InRange(0.15f, 1.4f), "Animation root height must not add a second jump above the physics capsule; a crouch can lower the hips.");
                    if (!captured && Mathf.Abs(body.linearVelocity.y) < 1) { Capture(capture); captured = true; }
                }
                yield return null;
            }
            Assert.That(sawClip, Is.True, "Expected the supplied " + clipName + " animation during the physical jump.");
            Assert.That(peak - startY, Is.InRange(1.05f, 1.4f));
            Assert.That(actor.GetComponent<PlayerMotor>().IsGrounded, Is.True);
            Assert.That(body.position.y, Is.EqualTo(startY).Within(0.08f));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"), Is.True);
        }

        [UnityTest]
        public IEnumerator RetargetedLocomotionKeepsModelAtPlayerAndWeaponAttached()
        {
            yield return CreateArena();
            var animator = actor.GetComponentInChildren<Animator>();
            Assert.That(animator.isHuman, Is.True); Assert.That(animator.applyRootMotion, Is.False);
            Capture("Idle");
            Press(keyboard.wKey); yield return new WaitForSeconds(1.4f);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 offset = actor.transform.InverseTransformPoint(hips.position);
            Assert.That(new Vector2(offset.x, offset.z).magnitude, Is.LessThan(0.4f), "The model must not walk away from its collider.");
            Assert.That(animator.GetFloat("MoveY"), Is.GreaterThan(0.8f));
            var view = actor.GetComponentInChildren<RifleView>();
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.That(Vector3.Distance(view.transform.position, hand.position), Is.LessThan(0.2f));
            Capture("Walk");
            Release(keyboard.wKey); yield return new WaitForSeconds(0.4f);
            actor.GetComponent<PlayerCameraRig>().SetViewAngles(0, 20); Press(mouse.rightButton);
            yield return new WaitForSeconds(0.5f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(1).IsName("Aim"), Is.True);
            Capture("Aim");
            Capture("AimSide");
            Assert.That(Vector3.Dot(view.transform.forward, Quaternion.Euler(20, 0, 0) * Vector3.forward), Is.GreaterThan(0.98f), "The barrel must follow the aiming direction despite different clip wrist rotations.");
            File.WriteAllText("Logs/PlayerAnimation/aim-direction.txt", $"rifleForward={view.transform.forward}; playerForward={actor.transform.forward}; muzzle={view.Muzzle.position - actor.transform.position}");
        }

        private void Capture(string name)
        {
            Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
            camera.transform.position = actor.transform.position + new Vector3(2.8f, 1.7f, 3.5f);
            if (name == "AimSide") camera.transform.position = actor.transform.position + new Vector3(4, 1.7f, 0.2f);
            camera.transform.LookAt(actor.transform.position + Vector3.up * 1.05f); camera.fieldOfView = 34;
            var rt = new RenderTexture(960, 720, 24); var pixels = new Texture2D(960, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); pixels.Apply(); Directory.CreateDirectory("Logs/PlayerAnimation");
                File.WriteAllBytes("Logs/PlayerAnimation/Player_" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous; rt.Release(); Object.Destroy(rt); Object.Destroy(pixels);
                camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = 60;
            }
        }
    }
}
#endif
