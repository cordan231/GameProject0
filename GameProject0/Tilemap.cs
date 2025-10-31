using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace GameProject0
{
    public class Tilemap
    {
        /// <summary>
        /// Dimensions of tiles and map
        /// </summary>
        int _tileWidth, _tileHeight, _mapWidth, _mapHeight;

        /// <summary>
        /// Tileset texture
        /// </summary>
        Texture2D _tilesetTexture;

        /// <summary>
        /// Tile info in the tileset
        /// </summary>
        Rectangle[] _tiles;

        /// <summary>
        /// Tilemap data
        /// </summary>
        int[] _map;

        /// <summary>
        /// Tilemap filename
        /// </summary>
        string _filename;

        public Tilemap(string filename)
        {
            _filename = filename;
        }

        public void LoadContent(ContentManager content)
        {
            string data = File.ReadAllText(Path.Join(content.RootDirectory, _filename));
            var lines = data.Split('\n');

            // First line is the tileset filename
            var tilesetFilename = lines[0].Trim();
            _tilesetTexture = content.Load<Texture2D>(tilesetFilename);

            // Second line is tile size
            var secondLine = lines[1].Split(',');
            _tileWidth = int.Parse(secondLine[0]);
            _tileHeight = int.Parse(secondLine[1]);

            // Tile bounds
            int tilesetColumns = _tilesetTexture.Width / _tileWidth;
            int tilesetRows = _tilesetTexture.Height / _tileHeight;
            _tiles = new Rectangle[tilesetColumns * tilesetRows];

            for (int y = 0; y < tilesetRows; y++)
            {
                for (int x = 0; x < tilesetColumns; x++)
                {
                    int index = y * tilesetColumns + x;
                    _tiles[index] = new Rectangle(
                        x * _tileWidth,
                        y * _tileHeight,
                        _tileWidth,
                        _tileHeight
                    );
                }
            }

            // Third line is map size
            var thirdLine = lines[2].Split(',');
            _mapWidth = int.Parse(thirdLine[0]);
            _mapHeight = int.Parse(thirdLine[1]);

            // Create map
            _map = new int[_mapWidth * _mapHeight];

            var mapDataLines = new List<string>();
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    mapDataLines.Add(line);
                }
            }

            var allMapData = string.Join(",", mapDataLines).Split(',');

            for (int i = 0; i < _mapWidth * _mapHeight; i++)
            {

                if (i < allMapData.Length && !string.IsNullOrWhiteSpace(allMapData[i]))
                {
                    _map[i] = int.Parse(allMapData[i]);
                }
                else
                {
                    _map[i] = 0;
                }
            }

        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    int index = _map[y * _mapWidth + x];
                    if (index == 0) continue;

                    spriteBatch.Draw(
                        _tilesetTexture,
                        new Vector2(
                            x * _tileWidth * 2.0f,
                            y * _tileHeight * 2.0f
                        ),
                        _tiles[index - 1],
                        Color.White,
                        0f,
                        Vector2.Zero,
                        2.0f,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }
    }
}