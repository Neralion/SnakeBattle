using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SnakeBattle;
using System.Drawing.Drawing2D;
using SnakeGenetic;

namespace Launcher
{
    public partial class BattlefieldForm : Form
    {
        private World world;
        private SystemInfoForm systemInfo;
        private SystemScoreForm systemScore;
        private BlockRenderer renderer;

        private Thread updateThread;
        private Thread renderThread;
        private Thread syncThread;

        public BattlefieldForm()
        {
            InitializeComponent();

            world = new World(
                Settings.BlokSize,
                Settings.FieldWidth,
                Settings.FieldHeight,
                Settings.TerrainDomainNumber,
                Settings.TerrainDomainPower,
                Settings.FoodCount);

            renderer = new BlockRenderer(world);

            ClientSize = world.SizeInPixels;

            updateThread = new Thread(DoUpdate) {IsBackground = true};
            renderThread = new Thread(DoRender) {IsBackground = true};
            syncThread = new Thread(DoSync) {IsBackground = true};

            updateThread.Start();
            renderThread.Start();
            syncThread.Start();

            systemScore?.Dispose();
            systemScore = new SystemScoreForm(world);
            systemScore.Show(this);

            systemInfo?.Dispose();
            systemInfo = new SystemInfoForm(world, renderer);
            systemInfo.Show(this);
        }

        private void DoSync()
        {
            while (true)
            {
                if (!updateThread.IsAlive && !renderThread.IsAlive)
                {
                    break;
                }

                Thread.Sleep(50);
            }

            world.Dispose();
            systemInfo.Dispose();
            renderer.Dispose();
        }

        private void DoRender()
        {
            while (!IsDisposed)
            {
                Invalidate();
                Thread.Sleep(Settings.RenderDelay);
            }
        }

        private void DoUpdate()
        {
            world.Startup();

            while (!IsDisposed)
            {
                world.Update();
                Thread.Sleep(Settings.UpdateDeleay);
            }
        }

        private static void DrawLinePointF(PaintEventArgs e)
        {
            var pos = RandomSnake.pos;
            pos.Y *= 7;
            pos.Y += 3;
            pos.X *= 7;
            pos.X += 3;
            var blackPen = new Pen(Color.Black, 2) {DashStyle = DashStyle.Dash};
            var redPen = new Pen(Color.Red, 2) {DashStyle = DashStyle.Dash};
            var bluePen = new Pen(Color.Blue, 2) {DashStyle = DashStyle.Dash};
            try
            {
                for (var i = 3; i < 41; i += 5)
                {
                    if ((int) RandomSnake.vision[i - 3] == 1)
                    {
                        e.Graphics.DrawLine(redPen, pos,
                            new Point((int) RandomSnake.vision[i] * 7 + 3,
                                (int) RandomSnake.vision[i + 1] * 7 + 3));
                        continue;
                    }

                    if ((int) RandomSnake.vision[i - 2] == 1)
                    {
                        e.Graphics.DrawLine(bluePen, pos,
                            new Point((int) RandomSnake.vision[i] * 7 + 3,
                                (int) RandomSnake.vision[i + 1] * 7 + 3));
                        continue;
                    }

                    e.Graphics.DrawLine(blackPen, pos,
                        new Point((int) RandomSnake.vision[i] * 7 + 3,
                            (int) RandomSnake.vision[i + 1] * 7 + 3));
                }
            }
            catch
            {
                // ignored
            }
        }


        private void BattlefieldForm_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.LightGray);
            renderer.Draw(e.Graphics);
            DrawLinePointF(e);
        }
    }
}