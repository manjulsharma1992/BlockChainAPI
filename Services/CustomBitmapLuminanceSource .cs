using System.Drawing;
using ZXing;
using ZXing.Common;

namespace MultiChainAPI.Services
{
public class CustomBitmapLuminanceSource : BaseLuminanceSource
{
    public CustomBitmapLuminanceSource(Bitmap bitmap)
        : base(bitmap.Width, bitmap.Height)
    {
        var luminanceData = CalculateLuminance(bitmap);

        // Set the protected 'luminances' field via reflection
        var luminanceField = typeof(BaseLuminanceSource)
            .GetField("luminances", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        luminanceField?.SetValue(this, luminanceData);
    }

    private static byte[] CalculateLuminance(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        byte[] data = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                byte luminance = (byte)((color.R + color.G + color.B) / 3);
                data[y * width + x] = luminance;
            }
        }

        return data;
    }

    // Required by BaseLuminanceSource
    protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height)
    {
        var source = new CustomBitmapLuminanceSource(new Bitmap(width, height));
        var luminanceField = typeof(BaseLuminanceSource)
            .GetField("luminances", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        luminanceField?.SetValue(source, newLuminances);
        return source;
    }
}

}
