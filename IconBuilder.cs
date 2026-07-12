using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconBuilder
{
    private static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };

    private static void Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : "屏幕常亮工具.ico";
        using (BinaryWriter writer = new BinaryWriter(File.Create(output)))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)Sizes.Length);

            int offset = 6 + 16 * Sizes.Length;
            byte[][] images = new byte[Sizes.Length][];
            for (int i = 0; i < Sizes.Length; i++)
                images[i] = MakeIconImage(Sizes[i]);

            for (int i = 0; i < Sizes.Length; i++)
            {
                int size = Sizes[i];
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }
            for (int i = 0; i < images.Length; i++) writer.Write(images[i]);
        }
    }

    private static byte[] MakeIconImage(int size)
    {
        using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            float scale = size / 256f;
            RectangleF canvas = new RectangleF(8 * scale, 8 * scale, 240 * scale, 240 * scale);
            using (GraphicsPath rounded = RoundedRectangle(canvas, 62 * scale))
            using (LinearGradientBrush background = new LinearGradientBrush(
                new PointF(30 * scale, 20 * scale), new PointF(225 * scale, 242 * scale),
                Color.FromArgb(13, 32, 67), Color.FromArgb(17, 113, 118)))
            {
                g.FillPath(background, rounded);
            }

            using (Pen border = new Pen(Color.FromArgb(92, 220, 252), Math.Max(1f, 3 * scale)))
                g.DrawPath(border, RoundedRectangle(canvas, 62 * scale));

            float x = 50 * scale, y = 64 * scale, w = 156 * scale, h = 103 * scale;
            using (GraphicsPath screen = RoundedRectangle(new RectangleF(x, y, w, h), 16 * scale))
            using (LinearGradientBrush fill = new LinearGradientBrush(
                new PointF(x, y), new PointF(x + w, y + h),
                Color.FromArgb(68, 196, 223), Color.FromArgb(34, 92, 175)))
            using (Pen stroke = new Pen(Color.FromArgb(210, 240, 255), Math.Max(1f, 7 * scale)))
            {
                g.FillPath(fill, screen);
                g.DrawPath(stroke, screen);
            }

            using (Pen beam = new Pen(Color.FromArgb(120, 235, 255), Math.Max(1f, 4 * scale)))
            {
                beam.StartCap = LineCap.Round;
                beam.EndCap = LineCap.Round;
                g.DrawLine(beam, 79 * scale, 97 * scale, 172 * scale, 97 * scale);
                g.DrawLine(beam, 79 * scale, 119 * scale, 146 * scale, 119 * scale);
            }

            using (Pen stand = new Pen(Color.FromArgb(232, 249, 255), Math.Max(1f, 8 * scale)))
            {
                stand.StartCap = LineCap.Round;
                stand.EndCap = LineCap.Round;
                g.DrawLine(stand, 128 * scale, 170 * scale, 128 * scale, 195 * scale);
                g.DrawLine(stand, 91 * scale, 198 * scale, 165 * scale, 198 * scale);
            }

            using (Brush glow = new SolidBrush(Color.FromArgb(255, 249, 196)))
                g.FillEllipse(glow, 181 * scale, 51 * scale, 25 * scale, 25 * scale);

            return ToIconBitmap(bitmap);
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
    {
        float d = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static byte[] ToIconBitmap(Bitmap bitmap)
    {
        int size = bitmap.Width;
        int pixelBytes = size * size * 4;
        int maskStride = ((size + 31) / 32) * 4;
        using (MemoryStream stream = new MemoryStream(40 + pixelBytes + maskStride * size))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(40);
            writer.Write(size);
            writer.Write(size * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(0);
            writer.Write(pixelBytes);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            for (int y = size - 1; y >= 0; y--)
                for (int x = 0; x < size; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    writer.Write(pixel.B);
                    writer.Write(pixel.G);
                    writer.Write(pixel.R);
                    writer.Write(pixel.A);
                }
            writer.Write(new byte[maskStride * size]);
            return stream.ToArray();
        }
    }
}
