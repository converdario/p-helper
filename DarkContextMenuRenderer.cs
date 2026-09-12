using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PHelper
{
    public class DarkContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkContextMenuRenderer() : base(new DarkColorTable())
        {
            this.ColorTable.UseSystemColors = false;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item == null || e.Graphics == null) return;

            if (e.Item.Selected && e.Item.Enabled)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(ColorTable.MenuItemSelected))
                {
                    Rectangle rect = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
                    using (GraphicsPath path = GetRoundedPath(rect, 4))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip == null || e.Graphics == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(ColorTable.MenuBorder, 1))
            {
                Rectangle rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                using (GraphicsPath path = GetRoundedPath(rect, 6))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            if (e.Item == null || e.Graphics == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(e.Item.Enabled ? Color.White : Color.FromArgb(130, 130, 130)))
            {
                int x = e.ArrowRectangle.X + e.ArrowRectangle.Width / 2 - 2;
                int y = e.ArrowRectangle.Y + e.ArrowRectangle.Height / 2 - 4;
                Point[] arrowPoints = { new Point(x, y), new Point(x + 4, y + 4), new Point(x, y + 8) };
                e.Graphics.FillPolygon(brush, arrowPoints);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item == null) return;

            e.TextColor = e.Item.Enabled ? Color.White : Color.FromArgb(130, 130, 130);
            var rect = e.TextRectangle;
            e.TextRectangle = new Rectangle(rect.X + 4, rect.Y, rect.Width, rect.Height);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Item == null || e.Graphics == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.White, 1.5f))
            {
                int x = e.ImageRectangle.X + 8;
                int y = e.ImageRectangle.Y + (e.ImageRectangle.Height / 2);
                Point[] checkPoints = { new Point(x, y), new Point(x + 3, y + 3), new Point(x + 8, y - 4) };
                e.Graphics.DrawLines(pen, checkPoints);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (e.Item == null || e.Graphics == null) return;

            using (var pen = new Pen(ColorTable.SeparatorDark))
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(pen, 32, y, e.Item.Width - 12, y);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(0, 120, 215); 
        public override Color MenuItemBorder => Color.Transparent;
        public override Color ToolStripDropDownBackground => Color.FromArgb(28, 28, 28); 
        public override Color ImageMarginGradientBegin => Color.FromArgb(28, 28, 28);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(28, 28, 28);
        public override Color ImageMarginGradientEnd => Color.FromArgb(28, 28, 28);
        public override Color SeparatorDark => Color.FromArgb(65, 65, 65);
        public override Color SeparatorLight => Color.Transparent;
        public override Color MenuBorder => Color.FromArgb(85, 85, 85); 
    }
}