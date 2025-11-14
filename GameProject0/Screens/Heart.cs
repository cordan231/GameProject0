using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameProject0
{
    public class Heart
    {
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private BasicEffect _effect;
        private Game _game;
        private Color _color;

        public Matrix World { get; set; } = Matrix.Identity;

        public Heart(Game game, Color color)
        {
            _game = game;
            _color = color;
            InitializeVertices();
            InitializeIndices();
            InitializeEffect();
        }

        private void InitializeVertices()
        {
            var darkColor = Color.Lerp(_color, Color.Black, 0.5f);

            var vertexData = new VertexPositionColor[]
            {
                // Front Face
                new VertexPositionColor(new Vector3(0, 0, 0), _color), // 0
                new VertexPositionColor(new Vector3(-1, 1, 0), _color), // 1
                new VertexPositionColor(new Vector3(-2, 0, 0), _color), // 2
                new VertexPositionColor(new Vector3(0, -2, 0), _color), // 3
                new VertexPositionColor(new Vector3(2, 0, 0), _color), // 4
                new VertexPositionColor(new Vector3(1, 1, 0), _color), // 5

                // Back Face
                new VertexPositionColor(new Vector3(0, 0, -1), darkColor), // 6
                new VertexPositionColor(new Vector3(-1, 1, -1), darkColor), // 7
                new VertexPositionColor(new Vector3(-2, 0, -1), darkColor), // 8
                new VertexPositionColor(new Vector3(0, -2, -1), darkColor), // 9
                new VertexPositionColor(new Vector3(2, 0, -1), darkColor), // 10
                new VertexPositionColor(new Vector3(1, 1, -1), darkColor), // 11
            };

            _vertexBuffer = new VertexBuffer(
                _game.GraphicsDevice,
                typeof(VertexPositionColor),
                vertexData.Length,
                BufferUsage.None
            );
            _vertexBuffer.SetData(vertexData);
        }

        private void InitializeIndices()
        {
            var indexData = new short[]
            {
                // Front Face
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 5,

                // Back Face
                6, 8, 7,
                6, 9, 8,
                6, 10, 9,
                6, 11, 10,

                // Sides
                1, 7, 2, 2, 7, 8, // Left Top
                2, 8, 3, 3, 8, 9, // Left Bottom
                3, 9, 4, 4, 9, 10, // Right Bottom
                4, 10, 5, 5, 10, 11, // Right Top
                5, 11, 1, 1, 11, 7 // Top
            };

            _indexBuffer = new IndexBuffer(
                _game.GraphicsDevice,
                IndexElementSize.SixteenBits,
                indexData.Length,
                BufferUsage.None
            );
            _indexBuffer.SetData(indexData);
        }

        private void InitializeEffect()
        {
            _effect = new BasicEffect(_game.GraphicsDevice);
            _effect.VertexColorEnabled = true;

            // Set up camera
            _effect.View = Matrix.CreateLookAt(
                new Vector3(0, 0, 10), // Camera position
                Vector3.Zero,          // Camera target
                Vector3.Up             // Up direction
            );

            _effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                _game.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                100.0f
            );
        }

        public void Draw()
        {
            _effect.World = this.World;
            _effect.CurrentTechnique.Passes[0].Apply();

            _game.GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            _game.GraphicsDevice.Indices = _indexBuffer;

            _game.GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                _indexBuffer.IndexCount / 3
            );
        }
    }
}