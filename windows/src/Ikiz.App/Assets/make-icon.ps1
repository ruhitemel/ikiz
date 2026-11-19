Add-Type -AssemblyName System.Drawing

$code = @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

public static class IkizIcon
{
    static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath();
        p.AddArc(x, y, r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Bitmap Render(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float s = size / 256f;

            using (var bg = RoundRect(8 * s, 8 * s, 240 * s, 240 * s, 54 * s))
            using (var brush = new LinearGradientBrush(new RectangleF(8 * s, 8 * s, 240 * s, 240 * s),
                       Color.FromArgb(255, 17, 34, 51), Color.FromArgb(255, 9, 19, 30), 55f))
            {
                g.FillPath(brush, bg);
            }

            using (var front = RoundRect(52 * s, 46 * s, 96 * s, 96 * s, 22 * s))
            using (var fill = new LinearGradientBrush(new RectangleF(52 * s, 46 * s, 96 * s, 96 * s),
                       Color.FromArgb(255, 45, 212, 191), Color.FromArgb(255, 13, 148, 136), 50f))
            {
                g.FillPath(fill, front);
            }

            using (var back = RoundRect(108 * s, 114 * s, 96 * s, 96 * s, 22 * s))
            using (var fill = new SolidBrush(Color.FromArgb(255, 10, 24, 36)))
            using (var pen = new Pen(Color.FromArgb(255, 94, 234, 212), 13 * s))
            {
                g.FillPath(fill, back);
                g.DrawPath(pen, back);
            }
        }
        return bmp;
    }

    public static void Save(string path)
    {
        int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
        var frames = new List<byte[]>();
        foreach (var size in sizes)
        {
            using (var bmp = Render(size))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                frames.Add(ms.ToArray());
            }
        }

        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int dim = sizes[i] >= 256 ? 0 : sizes[i];
                bw.Write((byte)dim);
                bw.Write((byte)dim);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write(frames[i].Length);
                bw.Write(offset);
                offset += frames[i].Length;
            }
            foreach (var f in frames) bw.Write(f);
        }
    }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$out = Join-Path $PSScriptRoot "ikiz.ico"
[IkizIcon]::Save($out)
"Yazildi: $out ($((Get-Item $out).Length) bayt)"
