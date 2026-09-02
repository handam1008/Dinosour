using UnityEngine;

namespace SSW
{
    /// <summary>
    /// Runtime-only neon feedback for the magician's cards.
    /// Every effect owns its own short-lived GameObject, so cards do not need to
    /// keep particle prefabs or clean up particles after they are destroyed.
    /// </summary>
    internal static class MagicianImpactVfx
    {
        const int EffectSortingOrder = 42;

        static readonly Color JokerPink = new Color(1f, 0.16f, 0.58f, 1f);
        static readonly Color JokerCyan = new Color(0.16f, 0.9f, 1f, 1f);
        static readonly Color JokerLime = new Color(0.45f, 1f, 0.25f, 1f);

        public static void SpawnLaunch(Vector3 position, Suit suit, Color color, float intensity, bool isJoker)
        {
            float scale = ClampIntensity(intensity);
            MagicianBloomRuntime.Ensure();

            GameObject root = CreateRoot("MagicianCardLaunchVfx", position);
            CreateCoreFlash(root.transform, color, 0.34f * scale, 0.12f, 4.4f);
            CreateExpandingRing(root.transform, color, 0.09f * scale, 0.46f * scale, 0.17f, 3.4f);

            switch (suit)
            {
                case Suit.Spade:
                    CreateSparks(root.transform, color, 5, 2.1f * scale, 4.1f * scale, 0.05f, 0.13f, true, 3.2f);
                    break;
                case Suit.Heart:
                    CreateRisingMotes(root.transform, color, 5, scale);
                    break;
                case Suit.Diamond:
                    CreateDiamondShards(root.transform, color, 5, scale, 0.45f);
                    break;
                default:
                    CreateCloverSwirl(root.transform, color, 6, scale, 0.42f);
                    break;
            }

            if (isJoker)
                CreateJokerBurst(root.transform, scale, false);

            DestroyAfter(root, 0.72f);
        }

        public static void SpawnImpact(
            Vector3 position,
            Suit suit,
            Color color,
            float intensity,
            bool isJoker,
            float effectRadius)
        {
            float scale = ClampIntensity(intensity);
            MagicianBloomRuntime.Ensure();

            GameObject root = CreateRoot("MagicianCardImpactVfx", position);
            CreateCoreFlash(root.transform, color, 0.54f * scale, 0.16f, 5.2f);
            if (suit != Suit.Diamond && effectRadius <= 0f)
                CreateExpandingRing(root.transform, color, 0.12f * scale, 0.8f * scale, 0.25f, 4f);

            switch (suit)
            {
                case Suit.Spade:
                    CreateSpadeImpact(root.transform, color, scale);
                    break;
                case Suit.Heart:
                    CreateHeartImpact(root.transform, color, scale);
                    break;
                case Suit.Diamond:
                    CreateDiamondImpact(root.transform, color, scale, effectRadius);
                    break;
                default:
                    CreateCloverImpact(root.transform, color, scale);
                    break;
            }

            // A joker may trigger Diamond as its bonus suit while keeping another
            // suit as its main color. effectRadius makes those extra shards travel
            // far enough to communicate that an area hit really happened.
            if (suit != Suit.Diamond && effectRadius > 0f)
                CreateDiamondImpact(root.transform, JokerCyan, scale, effectRadius);

            if (isJoker)
                CreateJokerBurst(root.transform, scale, true);

            DestroyAfter(root, suit == Suit.Diamond ? 1.15f : 0.9f);
        }

        public static void SpawnSurfaceImpact(
            Vector3 position,
            Suit suit,
            Color color,
            float intensity,
            bool isJoker)
        {
            float scale = ClampIntensity(intensity);
            MagicianBloomRuntime.Ensure();

            GameObject root = CreateRoot("MagicianCardSurfaceVfx", position);
            CreateCoreFlash(root.transform, color, 0.3f * scale, 0.11f, 4.3f);
            CreateSparks(
                root.transform,
                Color.Lerp(color, Color.white, 0.32f),
                7,
                1.4f * scale,
                3.5f * scale,
                0.035f,
                0.095f,
                true,
                4f);
            CreateSparks(
                root.transform,
                color,
                5,
                0.8f * scale,
                2.2f * scale,
                0.03f,
                0.07f,
                false,
                2.8f);

            switch (suit)
            {
                case Suit.Heart:
                    CreateRisingMotes(root.transform, color, 3, scale * 0.75f);
                    break;
                case Suit.Diamond:
                    CreateDiamondShards(root.transform, color, 4, scale * 0.8f, 0.25f);
                    break;
                case Suit.Clover:
                    CreateCloverSwirl(root.transform, color, 4, scale * 0.75f, 0.32f);
                    break;
            }

            if (isJoker)
                CreateJokerBurst(root.transform, scale * 0.6f, false);

            DestroyAfter(root, 0.68f);
        }

        static void CreateSpadeImpact(Transform parent, Color color, float scale)
        {
            CreateSparks(parent, Color.Lerp(color, Color.white, 0.28f), 13,
                3.3f * scale, 7.1f * scale, 0.055f, 0.18f, true, 4.5f);
            CreateSparks(parent, color, 7, 1.5f * scale, 3.2f * scale, 0.035f, 0.10f, true, 3.2f);
        }

        static void CreateHeartImpact(Transform parent, Color color, float scale)
        {
            CreateRisingMotes(parent, Color.Lerp(color, Color.white, 0.18f), 13, scale);
            CreateExpandingRing(parent, color, 0.14f * scale, 0.66f * scale, 0.34f, 3.1f);
        }

        static void CreateDiamondImpact(Transform parent, Color color, float scale, float effectRadius)
        {
            float radius = Mathf.Clamp(effectRadius, 0.35f, 4.5f);
            int shardCount = Mathf.Clamp(Mathf.RoundToInt(10f * scale + radius * 3f), 10, 24);

            // The gameplay radius still controls how far the burst travels, but
            // scattered streaks keep it from becoming one huge screen-covering ring.
            CreateSparks(
                parent,
                Color.Lerp(color, Color.white, 0.38f),
                shardCount,
                radius * 2.2f,
                radius * 4.8f,
                0.045f * scale,
                0.12f * scale,
                true,
                4.8f);
            CreateSparks(
                parent,
                color,
                Mathf.Max(6, shardCount / 2),
                radius * 1.2f,
                radius * 3.1f,
                0.035f * scale,
                0.085f * scale,
                false,
                3.5f);
            CreateDiamondShards(parent, color, shardCount, scale, radius);
        }

        static void CreateCloverImpact(Transform parent, Color color, float scale)
        {
            CreateCloverSwirl(parent, color, 16, scale, 0.85f);
            CreateSparks(parent, Color.Lerp(color, Color.white, 0.2f), 7,
                1.4f * scale, 3.4f * scale, 0.035f, 0.10f, false, 2.8f);
        }

        static void CreateCoreFlash(Transform parent, Color color, float diameter, float lifetime, float glow)
        {
            ParticleSystem system = CreateSystem(parent, "CoreFlash", MagicianGlowShape.Disc, glow);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = diameter;
            main.startColor = Color.Lerp(Color.white, color, 0.18f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = Curve(1.25f, 0.92f, 0.55f, 0f);
            ApplyFastFade(system, Color.white, color, 0.08f);
            system.Emit(1);
        }

        static void CreateExpandingRing(
            Transform parent,
            Color color,
            float startDiameter,
            float endDiameter,
            float lifetime,
            float glow)
        {
            ParticleSystem system = CreateSystem(parent, "NeonRing", MagicianGlowShape.Ring, glow);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = Mathf.Max(0.03f, endDiameter);
            main.startColor = Color.Lerp(color, Color.white, 0.32f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            float initial = Mathf.Clamp01(startDiameter / Mathf.Max(endDiameter, 0.03f));
            size.size = Curve(initial, 1.08f, 0.72f, 0f);
            ApplyFastFade(system, Color.white, color, 0.2f);
            system.Emit(1);
        }

        static void CreateSparks(
            Transform parent,
            Color color,
            int count,
            float minSpeed,
            float maxSpeed,
            float minSize,
            float maxSize,
            bool sharp,
            float glow)
        {
            ParticleSystem system = CreateSystem(parent, sharp ? "SharpStreaks" : "SparkBurst",
                sharp ? MagicianGlowShape.Stripe : MagicianGlowShape.Disc, glow);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.36f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.Lerp(color, Color.white, 0.25f);
            main.maxParticles = Mathf.Max(16, count + 4);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.045f;
            shape.radiusThickness = 1f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = Curve(0.8f, 1f, 0.35f, 0f);
            ApplyFastFade(system, Color.white, color, 0.3f);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            if (sharp && renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 1.9f;
                renderer.velocityScale = 0.14f;
            }

            system.Emit(count);
        }

        static void CreateRisingMotes(Transform parent, Color color, int count, float scale)
        {
            ParticleSystem system = CreateSystem(parent, "HeartMotes", MagicianGlowShape.Disc, 3.5f);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.58f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f * scale, 1.15f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f * scale, 0.105f * scale);
            main.startColor = Color.Lerp(color, Color.white, 0.2f);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.16f * scale;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.7f * scale, 1.5f * scale);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = Curve(0.45f, 1f, 0.82f, 0f);
            ApplyFastFade(system, Color.white, color, 0.45f);
            system.Emit(count);
        }

        static void CreateDiamondShards(Transform parent, Color color, int count, float scale, float spreadRadius)
        {
            ParticleSystem system = CreateSystem(parent, "DiamondShards", MagicianGlowShape.Diamond, 4.4f);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f * scale, 4.9f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f * scale, 0.13f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.Lerp(color, Color.white, 0.22f);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Mathf.Min(0.22f * Mathf.Max(1f, spreadRadius), 0.6f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = Curve(0.7f, 1.1f, 0.36f, 0f);
            ApplyFastFade(system, Color.white, color, 0.25f);
            system.Emit(count);
        }

        static void CreateCloverSwirl(Transform parent, Color color, int count, float scale, float radius)
        {
            ParticleSystem system = CreateSystem(parent, "CloverSwirl", MagicianGlowShape.Disc, 3.6f);
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.54f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f * scale, 0.95f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f * scale, 0.09f * scale);
            main.startColor = Color.Lerp(color, Color.white, 0.16f);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.22f;
            shape.radiusThickness = 1f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(2.8f, 5.6f);
            velocity.radial = new ParticleSystem.MinMaxCurve(0.4f * scale, 1.1f * scale);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = Curve(0.25f, 1f, 0.48f, 0f);
            ApplyFastFade(system, Color.white, color, 0.36f);
            system.Emit(count);
        }

        static void CreateJokerBurst(Transform parent, float scale, bool strong)
        {
            Color[] colors = { JokerPink, JokerCyan, JokerLime, new Color(1f, 0.8f, 0.16f, 1f) };
            int countPerColor = strong ? 5 : 3;
            float speedMultiplier = strong ? 1f : 0.65f;

            for (int i = 0; i < colors.Length; i++)
            {
                CreateSparks(parent, colors[i], countPerColor,
                    2.4f * scale * speedMultiplier, 5.3f * scale * speedMultiplier,
                    0.03f, 0.08f, i % 2 == 0, 4.2f);
            }

            if (strong)
                CreateExpandingRing(parent, Color.white, 0.16f * scale, 1.05f * scale, 0.32f, 5.1f);
        }

        static ParticleSystem CreateSystem(Transform parent, string name, MagicianGlowShape shape, float glow)
        {
            GameObject particleObject = new GameObject(name);
            particleObject.transform.SetParent(parent, false);

            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MagicianVfxMaterials.Get(shape, glow);
                renderer.sortingOrder = EffectSortingOrder;
            }

            system.Play();
            return system;
        }

        static void ApplyFastFade(ParticleSystem system, Color brightColor, Color tint, float colorHold)
        {
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(brightColor, 0f),
                    new GradientColorKey(tint, Mathf.Clamp01(colorHold)),
                    new GradientColorKey(tint, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.95f, Mathf.Clamp01(colorHold)),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;
        }

        static ParticleSystem.MinMaxCurve Curve(float first, float peak, float late, float end)
        {
            return new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, first),
                new Keyframe(0.2f, peak),
                new Keyframe(0.68f, late),
                new Keyframe(1f, end)));
        }

        static GameObject CreateRoot(string name, Vector3 position)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            return root;
        }

        static void DestroyAfter(GameObject root, float lifetime)
        {
            Object.Destroy(root, Mathf.Max(0.1f, lifetime));
        }

        static float ClampIntensity(float intensity)
        {
            return Mathf.Clamp(intensity, 0.35f, 3f);
        }
    }
}
