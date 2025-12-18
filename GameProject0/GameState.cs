using Microsoft.Xna.Framework;
using System.Collections.Generic;
using GameProject0.Enemies;

namespace GameProject0
{
    /// <summary>
    /// Tracks which enemy type should be spawned next.
    /// </summary>
    public enum SpawnState { Minotaur, Skeleton, None }

    /// <summary>
    /// A simple serializable class to store X,Y data
    /// </summary>
    public class VectorData
    {
        public float X { get; set; }
        public float Y { get; set; }

        public VectorData() { }
        public VectorData(Vector2 vec) { X = vec.X; Y = vec.Y; }
        public Vector2 ToVector2() => new Vector2(X, Y);
    }

    /// <summary>
    /// A helper class to store player-specific data.
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
    /// A helper class to store minotaur-specific data.
    /// </summary>
    public class MinotaurData
    {
        public VectorData Position { get; set; }
        public int Health { get; set; }
        public Direction Direction { get; set; }
        public bool IsRemoved { get; set; }
        public MinotaurState State { get; set; }
    }

    /// <summary>
    /// A helper class to store skeleton-specific data.
    /// </summary>
    public class SkeletonData
    {
        public VectorData Position { get; set; }
        public int Health { get; set; }
        public Direction Direction { get; set; }
        public bool IsRemoved { get; set; }
        public SkeletonState State { get; set; }
    }

    /// <summary>
    /// The main class that holds all data for a save file.
    /// </summary>
    public class GameState
    {
        public int Score { get; set; }
        public int PotionCount { get; set; }
        public int PotionProgress { get; set; }

        //public int NextPotionThreshold { get; set; }
        public int MinotaursKilledSinceBoss { get; set; }
        public int SkeletonsKilledSinceBoss { get; set; }
        public PlayerData Player { get; set; }
        public MinotaurData Minotaur { get; set; }
        public SkeletonData Skeleton { get; set; }
        //public List<VectorData> CoinPositions { get; set; }
        public SpawnState NextSpawn { get; set; }

        public GameState()
        {
            //CoinPositions = new List<VectorData>();
            Player = new PlayerData();
            Minotaur = new MinotaurData();
            Skeleton = new SkeletonData();
            NextSpawn = SpawnState.Minotaur;
        }
    }
}