using Microsoft.Xna.Framework;
using System.Collections.Generic;
using GameProject0.Enemies;

namespace GameProject0
{
    /// <summary>
    /// A helper class to store player-specific data.
    /// Needs public properties for JSON serialization.
    /// </summary>
    public class PlayerData
    {
        public Vector2 Position { get; set; }
        public int Health { get; set; }
        public CurrentState State { get; set; }
        public Vector2 KnockbackVelocity { get; set; }
    }

    /// <summary>
    /// A helper class to store enemy-specific data.
    /// </summary>
    public class EnemyData
    {
        public Vector2 Position { get; set; }
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
        public List<Vector2> CoinPositions { get; set; }

        public GameState()
        {
            CoinPositions = new List<Vector2>();
            Player = new PlayerData();
            Minotaur = new EnemyData();
        }
    }
}