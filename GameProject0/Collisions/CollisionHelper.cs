using Microsoft.Xna.Framework;
using System;

namespace GameProject0.Collisions
{
    public static class CollisionHelper
    {
        public static bool Collides(BoundingRectangle a, BoundingRectangle b)
        {
            return !(a.Right < b.Left || a.Left > b.Right ||
                     a.Top > b.Bottom || a.Bottom < b.Top);
        }

        public static bool Collides(BoundingCircle a, BoundingCircle b)
        {
            return Vector2.Distance(a.Center, b.Center) < a.Radius + b.Radius;
        }

        public static bool Collides(BoundingCircle c, BoundingRectangle r)
        {

            float closestX = Math.Clamp(c.Center.X, r.Left, r.Right);
            float closestY = Math.Clamp(c.Center.Y, r.Top, r.Bottom);

            float distanceX = c.Center.X - closestX;
            float distanceY = c.Center.Y - closestY;

            return (distanceX * distanceX) + (distanceY * distanceY) < (c.Radius * c.Radius);
        }
    }
}