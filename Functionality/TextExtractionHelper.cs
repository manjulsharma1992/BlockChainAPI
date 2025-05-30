using System.Security.Cryptography;
using System.Text;
using UglyToad.PdfPig;
using Tesseract;

public static class TextExtractionHelper
{
    public static string ExtractTextFromPdf(string pdfPath)
    {
        StringBuilder text = new StringBuilder();
        using (var pdf = PdfDocument.Open(pdfPath))
        {
            foreach (var page in pdf.GetPages())
            {
                text.Append(page.Text);
            }
        }
        return text.ToString();
    }

    public static string ExtractTextFromImage(string imagePath)
    {
        using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
        {
            using (var img = Pix.LoadFromFile(imagePath))
            {
                using (var page = engine.Process(img))
                {
                    return page.GetText();
                }
            }
        }
    }

    public static string ComputeSha256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
