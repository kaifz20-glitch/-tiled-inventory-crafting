using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// One-shot particle burst created entirely at runtime — used for the "successful
    /// craft" celebration so the demo needs no particle prefabs. Hook it (or your own
    /// VFX) to <c>CraftingSystem.JobCompleted</c>.
    /// </summary>
    public static class ParticleBurst
    {
        public static void Play(Vector3 position, Color color, int count = 26, float lifetime = 1.1f)
        {
            var go = new GameObject("CraftBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startColor = color;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startLifetime = lifetime;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = new ParticleSystem.MinMaxCurve(2.2f, 3.6f);
            velocity.space = ParticleSystemSimulationSpace.World;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            ps.Play();
            Object.Destroy(go, lifetime + 0.6f);
        }
    }
}
