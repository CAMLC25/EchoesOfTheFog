# Elena animations and prototype rifle

> The main Forest scene now starts unarmed and unlocks this rifle through an F pickup. Test scenes retain the equipped default. See [Forest-Opening.md](Forest-Opening.md) for the opening flow and objectives.

## Play

Open `Assets/Game/Scenes/Tests/AssetValidation.unity` for the existing forest map, or `Assets/Game/Scenes/Tests/CombatTest.unity` for three damageable capsule targets. The forest scenery is unchanged. Both scenes use the updated PlayerCapsule prefab.

- WASD: move; Shift: sprint; Space: jump; mouse: orbit.
- Right mouse: aim. Hold left mouse: fire, up to five shots per second.
- R: reload. A full magazine or an empty reserve cannot reload.
- Esc releases the pointer. The first click to recapture it does not fire; release and click again.
- Magazine: 30 rounds; reserve: 90; damage: 25; maximum ray distance: 100 m; reload: approximately 3.4 seconds. These are prototype values in `Assets/Game/Data/Weapons/PrototypeRifle.asset`.

CombatTest targets have 100 HP and change from green to red as they lose HP. They have no AI. Health zero disables firing/reloading and plays the death animation. Stop and restart Play to reset targets, health and ammunition.

## Assets and animation

The 20 imported Mixamo FBX clips use Humanoid with Elena's existing Avatar. Idle, locomotion and strafe clips loop. Translation is extracted and discarded because the Rigidbody motor owns movement; Animator root motion stays disabled. The original FBX binary files are preserved; their Unity importer settings change.

`Assets/Game/Animations/Player/Generated/ElenaRifle.controller` has a directional locomotion blend tree and an upper-body masked layer for rifle idle, firing, reload and hit reaction. Death uses a full-body clip. Left-hand IK holds the foregrip and releases during reload/death. The right hand owns the rifle socket.

Right mouse blends into a held firing pose for aiming. Hip fire also rotates the body toward camera yaw and points the barrel along camera yaw/pitch, without entering the zoomed aim mode. A short 0.45-second pose hold after a shot allows a single click to complete the turn. Firing blocks sprint but allows walking/strafing. Outside aim/fire, camera orbit remains independent of body rotation. The rifle socket compensates for differing wrist rotations before support-hand IK is solved. This is a prototype pose setup; fingers, extreme camera angles and transitions still benefit from manual animation review.

`Assets/Game/Prefabs/Weapons/PlayerRifle.prefab` now wraps the imported `Free Low Poly Weapons/Prefabs/Gun_31.prefab`, scaled to approximately 0.65 m long. The shared wrapper, muzzle and grip keep their asset identities so existing Player references remain valid. A separate Standard material uses the imported palette texture. Source weapon prefabs, meshes and materials are unchanged; the alternate Gun_10 remains available for later use.

Space now plays `Rifle Jump` while armed, with `Jump` available as an unarmed fallback. This does not add an equipment-toggle key. The existing AirHold state contains a blend tree selected by the Armed parameter. JumpPhase follows the Rigidbody's vertical velocity through takeoff, apex and descent; grounded returns to locomotion. Vertical root translation is extracted and discarded so the clip cannot add a second jump above the collider. The regular upper-body idle layer fades out during a jump, unless aim/fire/reload needs it. Reload uses the supplied animation, but does not detach the magazine or animate individual gun parts. No gunshot/reload audio was supplied.

Jump clips use Feet as the vertical root reference so the model remains anchored to the capsule while its pose changes, following Unity's [root motion settings](https://docs.unity.com/en-us/engine/6000.0/manual/animation-section/animation-mecanim/avatar-creationand-setup/root-motion). The 0.6-second Rifle Jump and 2.167-second Jump are sampled by physical flight phase rather than allowed to change the body's trajectory.

## Shooting

Hitscan uses the center of the main camera for aiming, then casts from the muzzle toward that point. A chest-to-muzzle check prevents a protruding barrel from bypassing cover. Raycasts exclude the Player layer and ignore triggers. Hits use the existing `IDamageable` interface. A short tracer, muzzle light and red hit-marker give feedback; there are no projectile physics or enemy systems.

`WeaponAmmo` conserves rounds when transferring reserve ammunition. Reload only transfers rounds on completion, blocks firing while active, and cancels on death or disabling the rifle. Esc releases controls while the reload timer continues.

## Maintenance and validation

`PlayerAnimationSetup.Apply` is an explicit batch-only setup tool; it does not run automatically when opening Unity. Close Unity before running it. It migrates the jump states and replaces the weapon visual with Gun_31 if needed, keeping the wrapper/muzzle/grip identities. It preserves CombatTest and existing combat values. Setup tools store local recovery copies under `.utmp/Backups`, outside Unity's imported Assets tree. Historical Assets/Archive copies and the unused default SampleScene were removed after reference checks; see [Project-Cleanup.md](Project-Cleanup.md).

Automated tests and rendered pose previews are recorded in `Logs/PlayerAnimation` (ignored by Git). Tests cover existing movement/jumping/map traversal, ammo conservation, damage and cover, fire rate, reload timing, click-to-recapture, R input, death and model/collider alignment. Rendered previews support visual review but do not replace manual playtesting of all aim angles and transitions.

Validation: the Jump/Gun_31 update passes 19/19 PlayMode and 23/23 EditMode tests, including camera turns during hip fire, fire while strafing, orbit after releasing fire, both jump clips with model/collider alignment, existing jump safeguards, damage/reload and the map walkthrough. Results are recorded in `jump-gun31-results.xml` and `jump-gun31-editmode.xml`; pose captures include Idle, Walk, Reload, Aim, AimSide, HipFire90, HipFire180, RifleJump and UnarmedJump. Source environment assets and the AssetValidation scene layout are unchanged. No commit or push is included in this integration.
