using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ParticleSystemExample;

namespace GameProject0.Particles
{
    public class BloodSplatterParticleSystem : ParticleSystem
    {
        public BloodSplatterParticleSystem(Game game, int maxParticles) : base(game, maxParticles)
        {
        }

        protected override void InitializeConstants()
        {
            textureFilename = "circle";
            minNumParticles = 15;
            maxNumParticles = 25;
            blendState = BlendState.AlphaBlend;
            DrawOrder = AlphaBlendDrawOrder;
        }

        protected override void InitializeParticle(ref Particle p, Vector2 where)
        {
            var velocity = RandomHelper.NextDirection() * RandomHelper.NextFloat(50, 250);
            var lifetime = RandomHelper.NextFloat(0.2f, 0.6f);
            var acceleration = new Vector2(0, 300); // Gravity
            var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);
            var angularVelocity = RandomHelper.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
            var scale = RandomHelper.NextFloat(0.5f, 1.5f);

            p.Initialize(where, velocity, acceleration, Color.Red, lifetime, scale, rotation, angularVelocity);
        }

        protected override void UpdateParticle(ref Particle particle, float dt)
        {
            base.UpdateParticle(ref particle, dt);

            float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;
            float alpha = 4 * normalizedLifetime * (1 - normalizedLifetime);
            particle.Color = Color.Red * alpha;
            particle.Scale *= 0.99f;
        }


        public void Splatter(Vector2 where)
        {
            AddParticles(where);
        }
    }
}

