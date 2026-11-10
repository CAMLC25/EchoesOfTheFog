using System;
using System.Globalization;
using System.IO;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Data;
using NUnit.Framework;
using UnityEngine;

namespace EchoesOfTheFog.Tests
{
    public sealed class HealthTests
    {
        private GameObject actor;
        private Health health;

        [SetUp]
        public void SetUp()
        {
            actor = new GameObject("Health test actor");
            health = actor.AddComponent<Health>();
            health.ResetHealth(100f);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(actor);

        [Test]
        public void LethalDamageEmitsDeathOnceAndCannotBeHealed()
        {
            int deaths = 0;
            health.Died += () => deaths++;
            health.TakeDamage(120f);
            health.TakeDamage(10f);
            health.Heal(100f);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsDead, Is.True);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void HealingClampsAndNotifiesWithCurrentAndMaximum()
        {
            health.TakeDamage(30f);
            float current = 0f, maximum = 0f;
            health.HealthChanged += (value, max) => { current = value; maximum = max; };
            health.Heal(200f);
            Assert.That(current, Is.EqualTo(100f));
            Assert.That(maximum, Is.EqualTo(100f));
        }

        [Test]
        public void ExplicitResetStartsANewLife()
        {
            health.TakeDamage(100f);
            health.ResetHealth(200f);
            Assert.That(health.IsDead, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(200f));
        }

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidAmountsDoNotCorruptState(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => health.TakeDamage(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.Heal(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.ResetHealth(value));
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
        }
    }

    public sealed class ConfigTests
    {
        private const string Runner = "Runner,40,4.5,10,18,1.5,1,5";
        private const string Tanker = "Tanker,200,2,30,12,2,2.5,7";
        private static StringReader Csv(string rows) => new StringReader(GameConfigLoader.Header + "\n" + rows);

        [Test]
        public void ShippedConfigLoadsFromStreamingAssets()
        {
            var configs = GameConfigLoader.LoadDefault();
            Assert.That(configs.Count, Is.EqualTo(2));
            Assert.That(configs["Runner"].MoveSpeed, Is.GreaterThan(configs["Tanker"].MoveSpeed));
            Assert.That(configs["Tanker"].Health, Is.GreaterThan(configs["Runner"].Health));
        }

        [Test]
        public void NumbersAreIndependentOfMachineLocale()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
                using (var reader = Csv(Runner + "\n\n" + Tanker))
                    Assert.That(GameConfigLoader.Parse(reader)["runner"].MoveSpeed, Is.EqualTo(4.5f));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCase("Runner,NaN,4.5,10,18,1.5,1,5")]
        [TestCase("Runner,Infinity,4.5,10,18,1.5,1,5")]
        [TestCase("Runner,-40,4.5,10,18,1.5,1,5")]
        [TestCase("Runner,40,4.5,10,18,1.5,0,5")]
        [TestCase("Runner,40,4.5,10,1,2,1,5")]
        [TestCase("Runner,40,4.5")]
        [TestCase(",40,4.5,10,18,1.5,1,5")]
        public void MalformedRowReportsItsLine(string row)
        {
            using (var reader = Csv(row + "\n" + Tanker))
            {
                var error = Assert.Throws<FormatException>(() => GameConfigLoader.Parse(reader));
                StringAssert.Contains("line 2", error.Message);
            }
        }

        [Test]
        public void DuplicateIdsAreRejectedIgnoringCase()
        {
            using (var reader = Csv(Runner + "\n" + Runner.ToLowerInvariant() + "\n" + Tanker))
                Assert.Throws<FormatException>(() => GameConfigLoader.Parse(reader));
        }

        [Test]
        public void MissingArchetypeIsRejected()
        {
            using (var reader = Csv(Runner))
                Assert.Throws<FormatException>(() => GameConfigLoader.Parse(reader));
        }

        [Test]
        public void WrongHeaderIsRejected()
        {
            using (var reader = new StringReader("Health,Id\n40,Runner"))
                Assert.Throws<FormatException>(() => GameConfigLoader.Parse(reader));
        }

        [Test]
        public void MissingFileIsNotSilentlyReplacedWithDefaults()
        {
            string path = Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".csv");
            Assert.Throws<FileNotFoundException>(() => GameConfigLoader.Load(path));
        }
    }
}
