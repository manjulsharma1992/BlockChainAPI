using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ZXing;

namespace MultiChainAPI.Services
{
   public class BitmapLuminanceSource : LuminanceSource
{
    private readonly byte[] luminance;

    public BitmapLuminanceSource(Bitmap bitmap)
        : base(bitmap.Width, bitmap.Height)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        luminance = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                byte gray = (byte)((0.3 * color.R) + (0.59 * color.G) + (0.11 * color.B));
                luminance[y * width + x] = gray;
            }
        }
    }

    public override byte[] Matrix => luminance;

    public override byte[] getRow(int y, byte[] row)
    {
        int width = Width;
        if (row == null || row.Length < width)
        {
            row = new byte[width];
        }

        Array.Copy(luminance, y * width, row, 0, width);
        return row;
    }
}
}