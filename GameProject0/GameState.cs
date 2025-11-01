using Microsoft.Xna.Framework;
using System.Collections.Generic;
using GameProject0.Enemies;

namespace GameProject0
{
    /// <summary>
    /// A simple serializable class to store X,Y data
    /// </summary>
    public class VectorData
    {
        public float X { get; set; }
        public float Y { get; set; }

        // Parameterless constructor for deserialization
        public VectorData() { }

        // Helper constructor to convert from Vector2
        public VectorData(Vector2 vec)
        {
            X = vec.X;
            Y = vec.Y;
        }

        // Helper method to convert back to Vector2
        public Vector2 ToVector2() => new Vector2(X, Y);
    }


    /// <summary>
    /// A helper class to store player-specific data.
    /// Needs public properties for JSON serialization.
    /// </summary>
    public class PlayerData
    {
        public VectorData Position { get; set; }
        public int Health { get; set; }
        public CurrentState State { get; set; }
        public VectorData KnockbackVelocity { get; set; }
        public Direction Direction { get; set; }
    }

    /// <summary>
    /// A helper class to store enemy-specific data.
    /// </summary>
    public class EnemyData
    {
        public VectorData Position { get; set; }
        public int Health { get; set; }
        public Direction Direction { get; set; }
        public bool IsRemoved { get; set; }
        public MinotaurState State { get; set; }
    }

    /// <summary>
    /// The main class that holds all data for a save file.
    /// </summary>
    public class GameState
    {
        public int Score { get; set; }
        public PlayerData Player { get; set; }
        public EnemyData Minotaur { get; set; }
        public List<VectorData> CoinPositions { get; set; }

        public GameState()
        {
            CoinPositions = new List<VectorData>();
            Player = new PlayerData();
            Minotaur = new EnemyData();
        }
    }
}