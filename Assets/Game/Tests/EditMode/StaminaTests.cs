using EchoesOfTheFog.Player;
using NUnit.Framework;
using UnityEngine;

namespace EchoesOfTheFog.Tests
{
    public sealed class StaminaTests
    {
        private GameObject actor;
        private PlayerStamina stamina;
        [SetUp] public void SetUp() { actor = new GameObject("Stamina"); stamina = actor.AddComponent<PlayerStamina>(); stamina.ResetStamina(); }
        [TearDown] public void TearDown() => Object.DestroyImmediate(actor);

        [Test]
        public void ExhaustionRequiresRecoveryThreshold()
        {
            stamina.Tick(true, 10);
            Assert.That(stamina.Current, Is.Zero);
            Assert.That(stamina.CanSprint, Is.False);
            stamina.Tick(false, 1.2f);
            Assert.That(stamina.Current, Is.Zero);
            stamina.Tick(false, 1);
            Assert.That(stamina.CanSprint, Is.False);
            stamina.Tick(false, 1);
            Assert.That(stamina.CanSprint, Is.True);
        }

        [Test]
        public void RecoveryOnlyUsesTimeAfterCooldownAndClampsAtMaximum()
        {
            stamina.Tick(true, 2);
            float depleted = stamina.Current;
            stamina.Tick(false, 1.7f);
            Assert.That(stamina.Current, Is.EqualTo(depleted + 9).Within(0.01f));
            stamina.Tick(false, 100);
            Assert.That(stamina.Current, Is.EqualTo(stamina.Maximum));
        }

        [Test]
        public void ResetRestoresAnExhaustedActor()
        {
            stamina.Tick(true, 10); stamina.ResetStamina();
            Assert.That(stamina.CanSprint, Is.True);
            Assert.That(stamina.Current, Is.EqualTo(stamina.Maximum));
        }
    }
}
