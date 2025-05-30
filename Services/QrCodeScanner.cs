using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using MultiChainAPI.Services;
using PdfiumViewer;
using ZXing;
using ZXing.Common;
using System.Drawing;
public static class QrCodeHelper
{

//  public static string ReadQrCodeAndGenerateHash(string imagePath)
// {
//     try
//     {
//         Console.WriteLine("Starting QR decode from image: " + imagePath);

//         using (var bitmap = (Bitmap)Bitmap.FromFile(imagePath))
//         {
//             var source = new BitmapLuminanceSource(bitmap);
//             var binarizer = new HybridBinarizer(source);
//             var binaryBitmap = new BinaryBitmap(binarizer);

//             Console.WriteLine($"Image dimensions: {bitmap.Width}x{bitmap.Height}");

//             var reader = new MultiFormatReader
//             {
//                 Hints = new Dictionary<DecodeHintType, object>
//                 {
//                     { DecodeHintType.TRY_HARDER, true },
//                     { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } }
//                 }
//             };

//             var result = reader.decode(binaryBitmap);
//             Console.WriteLine("Decoded Result: " + result?.Text);

//             if (result != null && !string.IsNullOrEmpty(result.Text))
//             {
//                 string qrText = result.Text;
//                 Console.WriteLine("QR Text: " + qrText);

//                 // if (qrText.Contains("MICRO") && qrText.Contains("UNIQUECODE"))
//                 // {
//                 //     int start = qrText.IndexOf("MICRO", StringComparison.OrdinalIgnoreCase) + "MICRO- ".Length;
//                 //     int end = qrText.IndexOf(" UNIQUECODE", StringComparison.OrdinalIgnoreCase);
//                 //     if (start < end)
//                 //     {
//                 //         string extracted = qrText.Substring(start, end - start).Trim(' ', '-', ':');
//                 //         Console.WriteLine("Extracted Code: " + extracted);
//                 //         return ComputeSha256Hash(extracted);
//                 //     }
//                 // }

//                 // Fallback: hash full QR content
//                 Console.WriteLine("Pattern not found. Hashing full QR content.");
//                 return ComputeSha256Hash(qrText);
//             }
//             else
//             {
//                 Console.WriteLine("No QR code found or result was empty.");
//             }
//         }
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine("Error decoding QR: " + ex.Message);
//     }

//     return string.Empty;
// }
public static string ReadQrCodeAndGenerateHash(string imagePath, bool scale = true)
{
    try
    {
        Console.WriteLine("📄 Starting QR decode from image: " + imagePath);

        using (var original = (Bitmap)Bitmap.FromFile(imagePath))
        {
            Console.WriteLine($"📐 Original image size: {original.Width}x{original.Height}");

            Bitmap bitmapToScan = original;

            // ✅ Only scale if image is smaller than threshold
            if (scale && (original.Width < 1200 || original.Height < 1200))
            {
                int factor = 3;
                bitmapToScan = new Bitmap(original, new Size(original.Width * factor, original.Height * factor));
                Console.WriteLine($"📈 Scaled image: {bitmapToScan.Width}x{bitmapToScan.Height}");
            }

            // ✅ Initialize reader with full options
            var reader = new ZXing.Windows.Compatibility.BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    ReturnCodabarStartEnd = true,
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE }
                }
            };

            // ✅ First scan attempt
            var result = reader.Decode(bitmapToScan);

            if (bitmapToScan != original)
                bitmapToScan.Dispose();

            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                Console.WriteLine("✅ QR Text: " + result.Text);
                return ComputeSha256Hash(result.Text);
            }
            else
            {
                Console.WriteLine("❌ No QR code found or result was empty.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("❗ Error decoding QR: " + ex.Message);
    }

    return string.Empty;
}



public static string ComputeSha256Hash(string rawData)
{
    using (SHA256 sha256Hash = SHA256.Create())
    {
        byte[] bytes = Encoding.Unicode.GetBytes(rawData); // Match SQL NVARCHAR
        byte[] hashBytes = sha256Hash.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
    }
}
public static List<Bitmap> ConvertPdfToImages(string pdfPath, int dpi = 400)
{
    var images = new List<Bitmap>();

    using (var document = PdfiumViewer.PdfDocument.Load(pdfPath))
    {
        for (int i = 0; i < document.PageCount; i++)
        {
            // ✅ Force high DPI rendering (400 DPI)
            var renderedImage = document.Render(i, dpi, dpi, false); // don't grayscale
            var bitmap = new Bitmap(renderedImage); // cast as full Bitmap
            images.Add(bitmap);

            Console.WriteLine($"Rendered page {i + 1} at {dpi} DPI → Image size: {bitmap.Width}x{bitmap.Height}");
        }
    }

    return images;
}

    //   public static string ComputeSha256Hash(string rawData)
    //         {
    //             using (SHA256 sha256Hash = SHA256.Create())
    //             {
    //                 byte[] bytes = Encoding.Unicode.GetBytes(rawData); // match SQL's NVARCHAR
    //                 byte[] hashBytes = sha256Hash.ComputeHash(bytes);

    //                 return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
    //             }
    //         }


}
