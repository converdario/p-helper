using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace PHelper
{
    public static class IconGenerator
    {

        public static Icon CreateTrayIcon(Color accentColor)
        {
            int size = 32;
            using Bitmap bmp = new Bitmap(size, size);
            
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                int cornerRadius = 8;
                Rectangle rect = new Rectangle(1, 1, size - 2, size - 2);
                
                using (GraphicsPath path = GetRoundedRectPath(rect, cornerRadius))
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    g.FillPath(brush, path);
                }

                using (Font font = new Font("Segoe UI Semibold", 12, FontStyle.Regular))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    
                    Rectangle textRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString("P", font, textBrush, textRect, sf);
                }
            }

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

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
}