using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Models;
using MultiChainAPI.Functionality;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using MultiChainAPI.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;
using Microsoft.Net.Http.Headers;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using MultiChainAPI.Models;
using System.Data;
using System.Security.Cryptography;
using iText.Kernel.Pdf.Canvas.Parser;
using Tesseract;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using Ghostscript.NET;
using UglyToad.PdfPig;
using PdfiumViewer;
using ZXing;
using MultiChainAPI.Services;
using MultiChainAPI.Data;
using Microsoft.EntityFrameworkCore;
using MultiChainAPI.Repository;



namespace MultiChainAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MultichainController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMultichainService _multichainService;
        private readonly string _connectionString;
        private readonly IReportService _reportService;
        private readonly IRabbitMqProducer _rabbitMqProducer;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ApplicationDbContext _context;


        public MultichainController(ApplicationDbContext context,IReportService reportService, IWebHostEnvironment webHostEnvironment, IMultichainService multichainService, IConfiguration configuration, IRabbitMqProducer rabbitMqProducer)
        {
            _configuration = configuration;
            _multichainService = multichainService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _rabbitMqProducer = rabbitMqProducer;
            //webHostEnvironment = webHostEnvironment;
            _reportService = reportService;
            _context=context;
  
        }
        [HttpPost("generateExamReport")]
        public async Task<IActionResult> GenerateExamReport([FromBody] ReportRequestModel model)
        {
            if (model == null)
                return BadRequest("Invalid request. Model is required.");
            // ✅ Decode the `collegeXml` to restore original XML format
            model.CollegeXml = System.Web.HttpUtility.HtmlDecode(model.CollegeXml);


            // ✅ Ensure `RollNo` is handled properly
            if (string.IsNullOrWhiteSpace(model.RollNo?.ToString()))
                model.RollNo = null;

            // **Call the async method to get DataSet**
            DataSet ds = await _reportService.GetExamReportDataAsync(model);
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            {
                return NotFound("No data found for the report.");
            }

            // **Generate the PDF asynchronously**
            byte[] pdfBytes = await _reportService.GenerateExamReportAsync(ds);
            return File(pdfBytes, "application/pdf", $"ExamReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }



        // [HttpPost("generate-tabulation")]
        // public async Task<IActionResult> GenerateReport([FromBody] ReportRequestModel model)
        // {
        //     if (model == null)
        //     {
        //         return BadRequest("Invalid request data.");
        //     }
        //     // Ensure nullable integers are handled correctly
        //     model.FromRollNo = string.IsNullOrWhiteSpace(model.FromRollNo?.ToString()) ? null : model.FromRollNo;
        //     model.ToRollNo = string.IsNullOrWhiteSpace(model.ToRollNo?.ToString()) ? null : model.ToRollNo;

        //     // Validate inputs
        //     if (string.IsNullOrEmpty(model.RollNo) || model.ExamTypeId <= 0 || model.SessionId <= 0)
        //     {
        //         return BadRequest("Missing required fields.");
        //     }

        //     // Check if the user is eligible
        //     // var isEligible = await _reportService.CheckStudentEligibility(model.DegreeId, model.CycleId, model.SubjectId, model.UserId);
        //     // if (!isEligible)
        //     // {
        //     //     return BadRequest("You are not eligible to download this report.");
        //     // }

        //     // Check if result is declared
        //     // var isResultDeclared = await _reportService.CheckResultDeclared(model.RollNo, model.CycleId, model.ExamTypeId, model.ExamConfigId, model.SubjectId, model.UserId);
        //     // if (!isResultDeclared)
        //     // {
        //     //     return BadRequest("Report cannot be downloaded as the result has not been declared.");
        //     // }

        //     // Remove unwanted newlines or spaces in XML before processing
        //     model.CollegeXml = model.CollegeXml.Replace("\n", "").Replace("\r", "").Trim();
        //     // Generate Report
        //     var reportFilePath = await _reportService.GenerateReportAsync(model);

        //     if (string.IsNullOrEmpty(reportFilePath))
        //     {
        //         return NotFound("Report not found!");
        //     }

        //     // Return File
        //     var fileBytes = await System.IO.File.ReadAllBytesAsync(reportFilePath);
        //     return File(fileBytes, "application/pdf", Path.GetFileName(reportFilePath));
        // }


        // // [HttpGet("generate-and-download")]
        // //     public IActionResult GenerateAndDownloadPdf()
        // //     {
        // //         try
        // //         {
        // //             // Generate timestamped filename
        // //             string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // //             string fileName = $"GeneratedPDF_{timestamp}.pdf";
        // //             string folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "pdfs");

        // //             // Ensure directory exists
        // //             if (!Directory.Exists(folderPath))
        // //                 Directory.CreateDirectory(folderPath);

        // //             string filePath = Path.Combine(folderPath, fileName);

        // //             // Create and save PDF
        // //             using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        // //             {
        // //                 using (PdfWriter writer = new PdfWriter(fileStream))
        // //                 {
        // //                     using (PdfDocument pdf = new PdfDocument(writer))
        // //                     {
        // //                         Document document = new Document(pdf);
        // //                         document.Add(new Paragraph("Hello, this is a generated PDF with a timestamp!"));
        // //                         document.Add(new Paragraph($"Generated on: {DateTime.Now}"));
        // //                     }
        // //                 }
        // //             }

        // //             // Read file and return as a downloadable response
        // //             byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
        // //             return File(fileBytes, "application/pdf", fileName);
        // //         }
        // //         catch (Exception ex)
        // //         {
        // //             return StatusCode(500, $"Error generating PDF: {ex.Message}");
        // //         }
        // //     }
        // // Search endpoint that mimics the Node.js '/search' functionality

        // [HttpGet("generate-and-download")]
        // public IActionResult GenerateAndDownloadPdf()
        // {
        //     try
        //     {
        //         // Generate timestamped filename
        //         string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        //         string fileName = $"GeneratedPDF_{timestamp}.pdf";

        //         // Get the solution directory (project root)
        //         string solutionDirectory = Directory.GetCurrentDirectory(); // Gets the root where the API is running
        //         string folderPath = Path.Combine(solutionDirectory, "Files");

        //         // Ensure directory exists
        //         if (!Directory.Exists(folderPath))
        //             Directory.CreateDirectory(folderPath);

        //         string filePath = Path.Combine(folderPath, fileName);

        //         // Create and save PDF
        //         using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        //         {
        //             using (PdfWriter writer = new PdfWriter(fileStream))
        //             {
        //                 using (PdfDocument pdf = new PdfDocument(writer))
        //                 {
        //                     Document document = new Document(pdf);
        //                     document.Add(new Paragraph("Hello, this is a generated PDF with a timestamp!"));
        //                     document.Add(new Paragraph($"Generated on: {DateTime.Now}"));
        //                 }
        //             }
        //         }

        //         // Read file and return as a downloadable response
        //         byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
        //         return File(fileBytes, "application/pdf", fileName);
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, $"Error generating PDF: {ex.Message}");
        //     }
        // }

        [HttpGet("search-key")]
        public async Task<IActionResult> SearchKeys([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest(new { success = false, message = "Search key is required." });
            }

            try
            {
                return Ok(new
                {
                    success = true,
                    results = key
                });
                //// Fetch all keys from MultiChain using the SendRpcRequestAsync method
                //var allKeysResponse = await _multichainService.FetchDataAsync("StudentMaster");

                //// Deserialize the response to get the keys
                //var allKeys = JsonConvert.DeserializeObject<dynamic>(allKeysResponse)?.result ?? new object[0];

                //// Create a list to hold filtered keys
                //var filteredKeys = new List<dynamic>();

                //// Manually filter the keys based on the provided key
                //foreach (var item in allKeys)
                //{
                //    if (item.key != null && item.key.ToString().Equals(key, StringComparison.OrdinalIgnoreCase))
                //    {
                //        filteredKeys.Add(item);
                //    }
                //}

                //if (filteredKeys.Any())
                //{
                //    return Ok(new
                //    {
                //        success = true,
                //        results = filteredKeys
                //    });
                //}
                //else
                //{
                //    return Ok(new
                //    {
                //        success = false,
                //        message = "No matching data found."
                //    });
                //}
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching data from MultiChain.",
                    error = ex.Message
                });
            }
        }
        [Authorize]
        [HttpGet("searchmaster-key")]
        public async Task<IActionResult> SearchMasterKeys([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest(new { success = false, message = "Search key is required." });
            }

            try
            {
                // Fetch all keys from MultiChain using the SendRpcRequestAsync method
                var command = await _multichainService.FetchMasterDataAsync("StudentMaster", key);

                // Deserialize the result into a strongly-typed object
                var parsedData = JsonConvert.DeserializeObject<MultiChainResponse>(command);

                // Check if result is available and not empty
                if (parsedData?.Result == null || parsedData.Result.Count == 0)
                {
                    return NotFound(new { success = false, message = "No data found for the provided key." });
                }
                // Filter out only confirmed items
                // var confirmedItems = parsedData.Result
                //     .Where(item => item.Confirmations > 0) // Ensure the item has confirmations
                //     .ToList();

                // // If no confirmed items exist, return a not found response
                // if (confirmedItems.Count == 0)
                // {
                //     return NotFound(new { success = false, message = "No confirmed data found for the provided key." });
                // }

                // Return the result as a JSON response
                return Ok(new { success = true, data = parsedData.Result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching data from MultiChain.",
                    error = ex.Message
                });
            }
        }

        //[Authorize]
        [HttpGet("searchexamdata-key")]
        public async Task<IActionResult> SearchExamDataKeys([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest(new { success = false, message = "Search key is required." });
            }

            try
            {
                // Fetch all keys from MultiChain using the SendRpcRequestAsync method
                var command = await _multichainService.FetchMasterDataAsync("studentmarksdata", key);

                // Deserialize the result into a strongly-typed object
                var parsedData = JsonConvert.DeserializeObject<MultiChainExamMarksResponse>(command);



                // Check if result is available and not empty
                if (parsedData?.Result == null || parsedData.Result.Count == 0)
                {
                    return NotFound(new { success = false, message = "No data found for the provided key." });
                }

                var distinctResults = parsedData?.Result
 ?.Distinct(new ExamMarksDataResultComparer())
 ?.ToList();

                // Return the result as a JSON response
                return Ok(new { success = true, data = distinctResults });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching data from MultiChain.",
                    error = ex.Message
                });
            }
        }

        public class ExamMarksDataResultComparer : IEqualityComparer<ExamMarksDataResult>
        {
            public bool Equals(ExamMarksDataResult x, ExamMarksDataResult y)
            {
                if (x?.Data?.Json == null || y?.Data?.Json == null) return false;

                return x.Data.Json.Rollno == y.Data.Json.Rollno &&
                       x.Data.Json.Cycle == y.Data.Json.Cycle &&
                       x.Data.Json.ExamType == y.Data.Json.ExamType &&
                       x.Data.Json.ExamMonthYear == y.Data.Json.ExamMonthYear &&
                       x.Data.Json.Paper == y.Data.Json.Paper &&
                       x.Data.Json.MarksObtained == y.Data.Json.MarksObtained &&
                       x.Data.Json.OperationType  == y.Data.Json.OperationType  &&
                       x.Data.Json.Allocid   == y.Data.Json.Allocid ;

            }

            public int GetHashCode(ExamMarksDataResult obj)
            {
                var json = obj.Data?.Json;
                return HashCode.Combine(json?.Rollno, json?.Cycle, json?.ExamType, json?.ExamMonthYear, json?.Paper, json?.MarksObtained, json?.OperationType, json?.Allocid);
            }
        }


        // Request Model
        public class FileVerificationRequest
        {
            public string Key { get; set; }
            public string FileHash { get; set; }
        }

       [HttpPost("publish-filedata")]
public async Task<IActionResult> PublishFileData([FromForm] IFormFile file, [FromForm] string rollno)
{
    if (string.IsNullOrEmpty(rollno) || file == null || file.Length == 0)
    {
        return BadRequest(new { success = false, message = "Roll No and File are required." });
    }

    try
    {
        // Save uploaded file temporarily
        var tempPath = Path.GetTempFileName();
        using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        string fileHash = string.Empty;

        if (file.ContentType == "application/pdf")
        {
            var images = QrCodeHelper.ConvertPdfToImages(tempPath, 400); // High-DPI

            if (images == null || images.Count == 0)
            {
                return BadRequest(new { success = false, message = "No images generated from PDF." });
            }

            string tessDataPath = @"E:\Multichain\tempuload";
            if (!Directory.Exists(tessDataPath))
            {
                Directory.CreateDirectory(tessDataPath);
            }

            int pageIndex = 1;
            foreach (var image in images)
            {
                string savedImagePath = Path.Combine(tessDataPath, $"page_{pageIndex}.png");
                image.Save(savedImagePath, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"📄 Saved page image: {savedImagePath}");

                // 👉 Scan the entire image without cropping (scale = true for better accuracy)
                fileHash = QrCodeHelper.ReadQrCodeAndGenerateHash(savedImagePath, scale: true);
                image.Dispose();

                if (!string.IsNullOrEmpty(fileHash))
                {
                    Console.WriteLine("✅ QR Code found. SHA-256 Hash: " + fileHash);
                    break;
                }

                pageIndex++;
            }
        }
        else
        {
            // 👉 Scan entire uploaded image directly
            Console.WriteLine("📷 Scanning uploaded image (full scan)...");
            fileHash = QrCodeHelper.ReadQrCodeAndGenerateHash(tempPath, scale: true);
        }

        // ✅ Return early if no QR found
        if (string.IsNullOrEmpty(fileHash))
        {
            Console.WriteLine("❌ No QR code detected.");
            return BadRequest(new
            {
                success = false,
                message = "No QR code found in the uploaded file."
            });
        }

        // Save file to permanent location
        var uploadPath = @"E:\Multichain\uploads";
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, file.FileName);
        System.IO.File.Move(tempPath, filePath);

        // Prepare message for RabbitMQ
        var message = new
        {
            streamName = "FileStream",
            keys = rollno,
            data = new
            {
                rollno,
                filehash = fileHash,
                filepath = filePath,
                Publishtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        await _rabbitMqProducer.SendDataToQueue(message, "sql_data_queue");

        return Ok(new
        {
            success = true,
            message = $"File uploaded & data sent to RabbitMQ successfully for Roll No: {rollno}.",
            filePath,
            filehash = fileHash
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "An error occurred.",
            error = ex.Message
        });
    }
}




//         [HttpPost("publish-filedata")]
// public async Task<IActionResult> PublishFileData([FromForm] IFormFile file, [FromForm] string rollno)
// {
//     if (string.IsNullOrEmpty(rollno) || file == null || file.Length == 0)
//     {
//         return BadRequest(new { success = false, message = "Roll No and File are required." });
//     }

//     try
//     {
//         // Save uploaded file temporarily
//         var tempPath = Path.GetTempFileName();
//         using (var stream = new FileStream(tempPath, FileMode.Create))
//         {
//             await file.CopyToAsync(stream);
//         }

//         string fileHash = string.Empty;

//         // Convert full PDF pages to full-size images (400 DPI)
//     if(file.ContentType == "application/pdf")
//         {
//         var images = QrCodeHelper.ConvertPdfToImages(tempPath,600);
        
//         if (images == null || images.Count == 0)
//         {
//             return BadRequest(new { success = false, message = "No images generated from PDF." });
//         }
//                 string tessDataPath = @"E:\Multichain\tempuload";
//                 if (!Directory.Exists(tessDataPath))
//                 {
//                     Directory.CreateDirectory(tessDataPath);
//                 }

//                 // Try scanning each full-page image for a QR code
//                 int pageIndex = 1;
//                 foreach (var image in images)
//                 {
//                     // Save full-page image locally for inspection/debugging
//                     string savedImagePath = Path.Combine(tessDataPath, $"page_{pageIndex}.png");
//                     image.Save(savedImagePath, System.Drawing.Imaging.ImageFormat.Png);
//                     Console.WriteLine($"Saved rendered page to: {savedImagePath}");

//                     // Continue temp image handling for QR scan
//                     string tempImagePath = Path.Combine(Path.GetTempPath(), $"qr_{Guid.NewGuid()}.png");
//                     image.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
//                     image.Dispose();

//                     Console.WriteLine("Scanning image: " + tempImagePath);
//                     fileHash = QrCodeHelper.ReadQrCodeAndGenerateHash(tempImagePath);
//                     System.IO.File.Delete(tempImagePath);

//                     if (!string.IsNullOrEmpty(fileHash))
//                     {
//                         Console.WriteLine("✅ QR Code found. SHA-256 Hash: " + fileHash);
//                         break;
//                     }

//                     pageIndex++;
//                 }

//         }
//         else{
            
//             fileHash = QrCodeHelper.ReadQrCodeAndGenerateHash(tempPath);



//         }

//                 if (string.IsNullOrEmpty(fileHash))
//         {
//             Console.WriteLine("❌ No QR code detected in PDF.");
//         }

//         // Save uploaded file to permanent storage
//         var uploadPath = @"E:\Multichain\uploads";
//         if (!Directory.Exists(uploadPath))
//             Directory.CreateDirectory(uploadPath);

//         var filePath = Path.Combine(uploadPath, file.FileName);
//         System.IO.File.Move(tempPath, filePath);

//         // Prepare message for RabbitMQ
//         var message = new
//         {
//             streamName = "FileStream",
//             keys = rollno,
//             data = new
//             {
//                 rollno,
//                 filehash = fileHash,
//                 filepath = filePath,
//                 Publishtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
//             }
//         };

//         await _rabbitMqProducer.SendDataToQueue(message, "sql_data_queue");

//         return Ok(new
//         {
//             success = true,
//             message = $"File uploaded & data sent to RabbitMQ successfully for Roll No: {rollno}.",
//             filePath,
//             filehash = fileHash
//         });
//     }
//     catch (Exception ex)
//     {
//         return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
//     }
// }





        // [HttpPost("publish-filedata")]
        // public async Task<IActionResult> PublishFileData([FromForm] IFormFile file, [FromForm] string rollno)
        // {
        //     if (string.IsNullOrEmpty(rollno) || file == null || file.Length == 0)
        //     {
        //         return BadRequest(new { success = false, message = "Roll No and File are required." });
        //     }

        //     try
        //     {
        //         // Save file temporarily
        //         var tempPath = Path.GetTempFileName();
        //         using (var stream = new FileStream(tempPath, FileMode.Create))
        //         {
        //             await file.CopyToAsync(stream);
        //         }
        //         string pdfText = "";
        //         string extractedText = "";
        //         string ocrText = "";
        //         string fileHash = "";

        //         // if (file.ContentType == "application/pdf")
        //         // {

        //         //     extractedText = ExtractTextFromPdf(tempPath);

        //         //     // 2. Convert to image and run OCR
        //         //     // var imagePaths = ConvertPdfToImages(tempPath); // Returns List<string>
        //         //     // foreach (var img in imagePaths)
        //         //     // {
        //         //     //     ocrText += ExtractTextFromImage(img) + " ";
        //         //     // }

        //         //     //           pdfText = $"{extractedText} {ocrText}";
        //         //     pdfText = extractedText;

        //         // }
        //         // else if (file.ContentType.StartsWith("image/")) // Handles image/jpeg, image/png, etc.
        //         // {
        //         //     pdfText = ExtractTextFromImage(tempPath);

        //         // }
        //         // else
        //         // {
        //         //     return BadRequest(new { success = false, message = "Unsupported file type. Only PDF and images are allowed." });
        //         // }

        //         // Extract text from PDF
        //         // string pdfText = ExtractTextFromPdf(tempPath);
        //         // Console.WriteLine(pdfText);

        //          extractedText = ExtractTextFromPdf(tempPath);

        //         //string cleanedText = CleanTextPreserveLettersAndNumbers(pdfText);


        //       string cleanedText = extractedText;//ExtractBetweenMicroAndUniqueCode(extractedText);
        //         // Console.WriteLine(cleanedText);

        //                Console.WriteLine("📄 Extracted PDF Text:");
        //         Console.WriteLine(cleanedText);
        //       fileHash = ComputeSha256Hash(cleanedText);

        //      // Console.WriteLine(extractedText);
        //         Console.WriteLine(fileHash);


        //         // Define upload directory
        //         var uploadPath = @"E:\Multichain\uploads";
        //         if (!Directory.Exists(uploadPath))
        //         {
        //             Directory.CreateDirectory(uploadPath);
        //         }

        //         // Save the file permanently
        //         var filePath = Path.Combine(uploadPath, file.FileName);
        //         System.IO.File.Move(tempPath, filePath);

        //         // Prepare message for RabbitMQ
        //         var message = new
        //         {
        //             streamName = "FileStream",
        //             keys = rollno,
        //             data = new
        //             {
        //                 rollno,
        //                 filehash = fileHash,
        //                 filepath = filePath,
        //                 Publishtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        //             }
        //         };

        //         // Send the message to RabbitMQ
        //         await _rabbitMqProducer.SendDataToQueue(message, "sql_data_queue");

        //         return Ok(new
        //         {
        //             success = true,
        //             message = $"File uploaded & data sent to RabbitMQ successfully for Roll No: {rollno}.",
        //             filePath,
        //             filehash = fileHash
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
        //     }
        // }


        public static string ExtractBetweenMicroAndUniqueCode(string fullText)
        {

            const string startToken = "MICRO-";
            const string endToken = "UNIQUECODE";

            int start = fullText.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);
            int end = fullText.IndexOf(endToken, StringComparison.OrdinalIgnoreCase);

            if (start >= 0 && end > start)
            {
                // Move start past "MICRO-"
                start += startToken.Length;

                // Get the substring between MICRO- and UNIQUECODE
                string extracted = fullText.Substring(start, end - start).Trim();
                Console.WriteLine($"[Extracted Between MICRO and UNIQUECODE]: {extracted}");
                return extracted;
            }

            Console.WriteLine("⚠️ MICRO or UNIQUECODE not found");
            return "";
        }


        public static string CleanTextPreserveLettersAndNumbers(string input)
        {
            string upper = input.ToUpperInvariant();
            return Regex.Replace(upper, @"[^A-Z0-9]", "");
        }







        public static string ExtractTextFromPdf(string filePath)
        {
            using (UglyToad.PdfPig.PdfDocument document = UglyToad.PdfPig.PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    var lines = page.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains("MICRO") && line.Contains("UNIQUECODE"))
                        {
                            int start = line.IndexOf("MICRO", StringComparison.OrdinalIgnoreCase) + "MICRO- ".Length;
                            int end = line.IndexOf(" UNIQUECODE", StringComparison.OrdinalIgnoreCase);
                            if (start < end)
                            {
                                return line.Substring(start, end - start).Trim(' ', '-', ':');
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }


        public static string ExtractTextFromImage(string imagePath)
        {
            var ocrText = "";

            using (var engine = new TesseractEngine(@"E:\Multichain\tessdata", "eng", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    using (var page = engine.Process(img))
                    {
                        ocrText = page.GetText();
                    }
                }
            }

            // Now extract the line between MICRO and UNIQUECODE
            var lines = ocrText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("MICRO") && line.Contains("UNIQUECODE"))
                {
                    int start = line.IndexOf("MICRO", StringComparison.OrdinalIgnoreCase) + "MICRO- ".Length;
                    int end = line.IndexOf(" UNIQUECODE", StringComparison.OrdinalIgnoreCase);
                    if (start < end)
                    {
                        return line.Substring(start, end - start).Trim(' ', '-', ':');
                    }
                }
            }

            return string.Empty;
        }


        //         public static string ExtractTextFromImage(string imagePath)
        // {
        //     var ocrText = "";
        //     using (var engine = new TesseractEngine(@"E:\Multichain\tessdata", "eng", EngineMode.Default))
        //     {
        //         using (var img = Pix.LoadFromFile(imagePath))
        //         {
        //             using (var page = engine.Process(img))
        //             {
        //                 ocrText = page.GetText();
        //             }
        //         }
        //     }
        //     return ocrText;
        // }




        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = Encoding.Unicode.GetBytes(rawData); // match SQL's NVARCHAR
                byte[] hashBytes = sha256Hash.ComputeHash(bytes);

                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            }
        }





       [HttpPost("verify-file")]
public async Task<IActionResult> VerifyFile([FromForm] IFormFile file, [FromForm] string key)
{
    if (file == null || file.Length == 0 || string.IsNullOrEmpty(key))
    {
        return BadRequest(new { success = false, message = "Key and file are required." });
    }

    try
    {
        // Save uploaded file temporarily
        var tempPath = Path.GetTempFileName();
        using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        string fileHash = "";

        // ✅ PDF: Convert to images, scan each full page (no crop)
        if (file.ContentType == "application/pdf")
        {
            var images = QrCodeHelper.ConvertPdfToImages(tempPath, 400);
            if (images == null || images.Count == 0)
            {
                return BadRequest(new { success = false, message = "No images generated from PDF." });
            }

            foreach (var image in images)
            {
                var tempImagePath = Path.Combine(Path.GetTempPath(), $"qr_{Guid.NewGuid()}.png");
                image.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
                image.Dispose();

                fileHash = QrCodeHelper.ReadQrCodeAndGenerateHash(tempImagePath, scale: true);
                System.IO.File.Delete(tempImagePath);

                if (!string.IsNullOrEmpty(fileHash))
                {
                    Console.WriteLine("✅ QR Code found. SHA-256 Hash: " + fileHash);
                    break;
                }
            }

            if (string.IsNullOrEmpty(fileHash))
            {
                Console.WriteLine("❌ No QR code detected in PDF.");
                return BadRequest(new { success = false, message = "No QR code found in the PDF file." });
            }
        }
        // ✅ Image: Direct full scan
        else if (file.ContentType.StartsWith("image/"))
        {
            Console.WriteLine("📷 Scanning image file...");
            fileHash = QrCodeHelper.ReadQrCodeAndGenerateHash(tempPath, scale: true);

            if (string.IsNullOrEmpty(fileHash))
            {
                Console.WriteLine("❌ No QR code detected in image.");
                return BadRequest(new { success = false, message = "No QR code found in the image file." });
            }
        }
        else
        {
            return BadRequest(new { success = false, message = "Unsupported file type." });
        }

        // ✅ Fetch data from MultiChain
        var command = await _multichainService.FetchMasterDataforpdfAsync("FileStream", key);
        var parsedData = JsonConvert.DeserializeObject<FileVerification>(command);

        if (parsedData?.Result == null || parsedData.Result.Count == 0)
        {
            return NotFound(new { success = false, message = "Key not found in MultiChain." });
        }

        foreach (var item in parsedData.Result)
        {
            if (item.Data?.Json != null)
            {
                string storedHash = item.Data.Json.FileHash;
                string storedFilePath = item.Data.Json.FilePath;
                Console.WriteLine($"Scanned Hash: {fileHash}");
                Console.WriteLine($"Stored Hash: {storedHash}");

                if (storedHash == fileHash)
                {
                    string fileName = Path.GetFileName(storedFilePath);
                    string fileUrl = $"http://localhost:5232/api/files/download/{fileName}";

                    return Ok(new
                    {
                        success = true,
                        message = "✅ File Verified!",
                        fileName,
                        fileUrl
                    });
                }
            }
        }

        return Ok(new { success = false, message = "❌ File hash does not match." });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "⚠️ Error verifying file.",
            error = ex.Message
        });
    }
}




        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            var filePath = Path.Combine(@"E:\Multichain\uploads", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { success = false, message = "❌ File not found!" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }



        public class FileVerification
        {
            [JsonProperty("result")]
            public List<FileDatItem> Result { get; set; }
        }

        public class FileDatItem
        {
            [JsonProperty("publishers")]
            public List<string> Publishers { get; set; }

            [JsonProperty("keys")]
            public List<string> Keys { get; set; }

            [JsonProperty("offchain")]
            public bool Offchain { get; set; }

            [JsonProperty("available")]
            public bool Available { get; set; }

            [JsonProperty("data")]
            public DataContainer Data { get; set; }

            [JsonProperty("confirmations")]
            public int Confirmations { get; set; }

            [JsonProperty("blocktime")]
            public long BlockTime { get; set; }

            [JsonProperty("txid")]
            public string TxId { get; set; }
        }

        public class DataContainer
        {
            [JsonProperty("json")]
            public ExamFileData Json { get; set; }
        }

        public class ExamFileData
        {
            [JsonProperty("rollno")]
            public string RollNo { get; set; }

            [JsonProperty("filehash")]
            public string FileHash { get; set; }

            [JsonProperty("filepath")]
            public string FilePath { get; set; }

            [JsonProperty("Publishtime")]
            public long PublishTime { get; set; }
        }




        [HttpGet("latest-transactions")]
        public async Task<IActionResult> GetLatestTransactions()
        {
            try
            {
                var transactionsJson = await _multichainService.FetchTransinfo();

                // Deserialize JSON
                var response = JsonConvert.DeserializeObject<MultiChainTransactionsResponse>(transactionsJson);

                if (response?.Result == null)
                {
                    return NotFound(new { success = false, message = "No transactions found" });
                }

                // Extract latest two transactions with timestamp
                var latestTransactions = response.Result.Select(tx => new
                {
                    tx.Txid,
                    tx.Time
                }).ToList();

                Console.WriteLine(latestTransactions);

                return Ok(new { success = true, latestTransactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching transactions.",
                    error = ex.Message
                });
            }
        }


        [HttpGet("transactions/last5days")]
        public async Task<IActionResult> GetLastFiveDaysTransactions()
        {
            try
            {
                var transactionsJson = await _multichainService.FetchLastFiveDaysTransactions();

                // Deserialize JSON
                var response = JsonConvert.DeserializeObject<MultiChainTransactionsResponse>(transactionsJson);

                if (response?.Result == null)
                {
                    return NotFound(new { success = false, message = "No transactions found" });
                }

                long fiveDaysAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
                // Extract latest two transactions with timestamp
                var latestTransactions1 = response.Result.Select(tx => new
                {
                    tx.Txid,
                    tx.Time

                }).ToList();



                var latestTransactions = latestTransactions1
                    .Where(t => t.Time >= fiveDaysAgo)
                    .OrderByDescending(t => t.Time)
                    .ToList();

                // Convert Unix timestamps to Date (UTC) and group by date
                var transactionCountsByDate = latestTransactions
                    .GroupBy(t => DateTimeOffset.FromUnixTimeSeconds(t.Time).UtcDateTime.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

                //Console.WriteLine(latestTransactions);
                // Console.WriteLine(fiveDaysAgo);

                return Ok(new { success = true, transactionCountsByDate });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching transactions.",
                    error = ex.Message
                });
            }
        }


        [HttpGet("getinfo")]
        public async Task<IActionResult> GetInfo()
        {

            try
            {
                // Fetch all keys from MultiChain using the SendRpcRequestAsync method
                var command = await _multichainService.FetchChaininfo();
                var peerinfo = await _multichainService.FetchPeerinfo();


                // Deserialize the result into a strongly-typed object
                Console.WriteLine("Received JSON: " + command);
                // Console.WriteLine(peerinfo);

                var parsedData = JsonConvert.DeserializeObject<ChainDataResult>(command);
                var parsedData1 = JsonConvert.DeserializeObject<MultiResponse>(peerinfo);



                if (parsedData?.Result == null || parsedData1?.Result == null)
                {
                    return NotFound(new { success = false, message = "No data found" });
                }
                // Extract peer count
                int peerCount = parsedData1.Result.Connections;
                // Console.WriteLine($"Peer Count: {peerCount}");

                // Return the result as a JSON response
                return Ok(new { success = true, data = parsedData.Result, data1 = peerCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching data from MultiChain.",
                    error = ex.Message
                });
            }
        }




        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT Id, Email, Password FROM BlockchainUsers WHERE Email = @Username";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", request.Username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var userId = reader["Id"].ToString();
                                var storedHash = reader["Password"].ToString();

                                // Verify password
                                if (BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
                                {
                                    // Generate JWT Token
                                    var token = GenerateJwtToken(userId, request.Username);
                                    return Ok(new { token });
                                }
                            }
                        }
                    }
                }

                return Unauthorized(new { message = "Invalid username or password." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        private string GenerateJwtToken(string userId, string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class Transaction
        {
            [JsonProperty("txid")]
            public string Txid { get; set; }

            [JsonProperty("time")]
            public long Time { get; set; } // Unix timestamp

            [JsonProperty("account")]
            public string Account { get; set; }

            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; } // "send" or "receive"

            [JsonProperty("amount")]
            public decimal Amount { get; set; }

            [JsonProperty("vout")]
            public int Vout { get; set; }
        }

        public class MultiChainTransactionsResponse
        {
            [JsonProperty("result")]
            public List<Transaction> Result { get; set; }



        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }


        [HttpPost("Student")]
        public async Task<IActionResult> CreateStudent([FromBody] Student student)
        {

            if (student is null)
            {

                return BadRequest("NO data");

            }
            await _context.TestStudents.AddAsync(student);
            await _context.SaveChangesAsync();
            return Ok();


        }

        [HttpGet("Employee")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetallEmployee()
        {

          var employee = await _context.Employees.ToListAsync();
          return Ok(employee);


        }
        

        [HttpGet("GetStudent")]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudent()
        {

          
            var student= await _context.TestStudents.ToListAsync();
            return Ok (student);


        }

    //     [HttpGet("{getbyid}")]
    // public ActionResult<Student> Get(int id)
    // {
    //     var student = _studentservice.GetStudentbyid(id);
    //     if (student == null)
    //         return NotFound();

    //     return Ok(student);
    // }

    //     [HttpDelete("{id}")]
    //     public async Task<IActionResult> Deletestudent(int id)
    //     {

    //         var studentid =await _context.TestStudents.FindAsync(id);

    //           if(studentid == null)
    //         {

    //             return NotFound();
    //         }

    //          _context.TestStudents.Remove(studentid);
    //         await _context.SaveChangesAsync();

            

    //         return Ok();



    //     }

    //     [HttpPut("{id}")]
    //     public async Task<IActionResult> UpdateStudent(int id,Student student)
    //     {
    //         if(id != student.ID)
    //         {
    //             return BadRequest("wrong id");
    //         }

    //         var data = await _context.TestStudents.FindAsync(id);

    //         if(data == null)
    //         {

    //             return NotFound();
    //         }

    //         data.regno= student.regno;
    //         data.ABCID=student.ABCID;

    //         await _context.SaveChangesAsync();

    //         return Ok();

    //     }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { success = false, message = "Email and password are required." });
            }

            try
            {
                // Hash password (for security)
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);


                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO BlockchainUsers (FullName, Email, Password) VALUES (@FullName, @Email, @Password)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", request.FullName);
                        command.Parameters.AddWithValue("@Email", request.Email);
                        command.Parameters.AddWithValue("@Password", hashedPassword);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Ok(new { success = true, message = "User registered successfully." });
                        }
                        else
                        {
                            return StatusCode(500, new { success = false, message = "User registration failed." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        // Ankit --START----
        //  [HttpPost("publish-filedata")]
        // public async Task<IActionResult> PublishFileData([FromBody] FileData request)
        // {
        //     if (string.IsNullOrEmpty(request.rollno) || string.IsNullOrEmpty(request.filehash) ||
        //      string.IsNullOrEmpty(request.filepath))
        //     {
        //         return BadRequest(new { success = false, message = "key,filehash,filepath are required." });
        //     }

        //     try
        //     {
        //          var errors = new List<ErrorResponse>();
        //          var successResults = new List<SuccessResponse>();
        //          try
        //             {
        //                 var data = new
        //             {
        //                 request.rollno,
        //                 request.filehash,
        //                 request.filepath,
        //                 Publishtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        //             };

        //                 // Prepare the message
        //                 var message = new
        //                 {
        //                     streamName = "FileStream",
        //                     keys = request.rollno,
        //                     data = data
        //                 };

        //                 // Send the message to RabbitMQ
        //                 Console.WriteLine(message);
        //                 await _rabbitMqProducer.SendDataToQueue(message, "sql_data_queue");

        //                 successResults.Add(new SuccessResponse
        //                 {
        //                     regno = request.rollno,
        //                     Message = $"Successfully sent data for rollno {request.rollno} to RabbitMQ.",
        //                 });
        //             }
        //             catch (Exception ex)
        //             {
        //                 errors.Add(new ErrorResponse
        //                 {
        //                     regno = request.rollno,
        //                     Message = $"Error processing data for regno {request.rollno}: {ex.Message}"
        //                 });
        //             }


        //         // Return appropriate response based on the results
        //         if (errors.Any())
        //         {
        //             return StatusCode(500, new
        //             {
        //                 success = false,
        //                 message = "Some records failed to process.",
        //                 errors,
        //                 successResults
        //             });
        //         }
        //         else
        //         {
        //             return Ok(new
        //             {
        //                 success = true,
        //                 message = "All data sent to RabbitMQ for processing.",
        //                 successResults
        //             });
        //         }

        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
        //     }
        // }

        // }



        // Ankit --END---

        public class PdfDataModel
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public DateTime CreatedOn { get; set; }
        }
        // [HttpGet("generate")]
        // public IActionResult GeneratePdf()
        // {
        //     var pdfData = new PdfDataModel
        //     {
        //         Title = "Sample PDF",
        //         Content = "This is a sample PDF content generated dynamically.",
        //         CreatedOn = DateTime.UtcNow
        //     };

        //     using (MemoryStream stream = new MemoryStream())
        //     {
        //         using (PdfWriter writer = new PdfWriter(stream))
        //         {
        //             using (PdfDocument pdf = new PdfDocument(writer))
        //             {
        //                 Document document = new Document(pdf);
        //                 document.Add(new Paragraph(pdfData.Title));
        //                 document.Add(new Paragraph(pdfData.Content));
        //                 document.Add(new Paragraph($"Generated on: {pdfData.CreatedOn}"));
        //             }
        //         }

        //         byte[] pdfBytes = stream.ToArray();
        //         return File(pdfBytes, "application/pdf", "Generated.pdf");
        //     }
        // }



        [HttpGet("download-pdf")]
        public IActionResult DownloadPdf()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "sample.pdf");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", "sample.pdf");
        }


        [HttpGet("fetch-data")]
        public async Task<IActionResult> FetchData()
        {
            var students = new List<Student>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Open the connection to the database
                    await connection.OpenAsync();

                    // Define your SQL query
                    var query = "SELECT TOP 10 regno, fullname, fname, mname, enrollmentno, C_Mobile, C_Address, C_Pincode, AdhaarNo FROM SMS_STUDENt_MST WHERE fk_degreeid = 3";

                    // Execute the query
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read the data from the database
                            while (await reader.ReadAsync())
                            {
                                students.Add(new Student
                                {
                                    regno = reader["regno"].ToString(),
                                    fullname = reader["fullname"].ToString(),
                                    fname = reader["fname"].ToString(),
                                    mname = reader["mname"].ToString(),
                                    enrollmentno = reader["enrollmentno"].ToString(),
                                    C_Mobile = reader["C_Mobile"].ToString(),
                                    C_Address = reader["C_Address"].ToString(),
                                    C_Pincode = reader["C_Pincode"].ToString(),
                                    AdhaarNo = reader["AdhaarNo"].ToString()
                                });
                            }
                        }
                    }
                }

                // Return the data as JSON
                return Ok(new
                {
                    success = true,
                    data = students
                });
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching data from the database.",
                    error = ex.Message
                });
            }
        }


        [HttpPost("publish-studentmaster")]
        public async Task<IActionResult> PublishData([FromBody] PublishDataRequest request)
        {
            if (request == null || request.Data == null || request.Keys == null || !request.Keys.Any())
            {
                return BadRequest("keys, and Data must be provided.");
            }

            // Filter out duplicate records based on regno and enrollmentno
            var uniqueRecords = new List<DataRecord>();
            var seenKeys = new HashSet<string>(); // Set to track already seen regno + enrollmentno

            foreach (var row in request.Data)
            {
                var uniqueKey = $"{row.regno}-{row.enrollmentNo}"; // Combining regno and enrollmentno to make sure they are unique
                if (!seenKeys.Contains(uniqueKey))
                {
                    uniqueRecords.Add(row);
                    seenKeys.Add(uniqueKey);
                }
            }

            var errors = new List<ErrorResponse>();
            var successResults = new List<SuccessResponse>();

            try
            {
                // Send each record to RabbitMQ for processing in the worker (Node.js)
                foreach (var row in uniqueRecords)
                {
                    var data = new
                    {
                        row.regno,
                        row.fullname,
                        row.fname,
                        row.mname,
                        row.enrollmentNo,
                        row.C_Mobile,
                        row.C_Address,
                        row.C_Pincode,
                        row.AdhaarNo,
                        Version = 1,
                        Publishtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    try
                    {
                        // Prepare the message
                        var message = new
                        {
                            streamName = "StudentMaster",
                            keys = row.regno,
                            data = data
                        };

                        // Send the message to RabbitMQ
                        Console.WriteLine(message);
                        await _rabbitMqProducer.SendDataToQueue(message, "StudentQueue");

                        successResults.Add(new SuccessResponse
                        {
                            regno = row.regno,
                            Message = $"Successfully sent data for regno {row.regno} to RabbitMQ.",
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ErrorResponse
                        {
                            regno = row.regno,
                            Message = $"Error processing data for regno {row.regno}: {ex.Message}"
                        });
                    }
                }

                // Return appropriate response based on the results
                if (errors.Any())
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Some records failed to process.",
                        errors,
                        successResults
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = true,
                        message = "All data sent to RabbitMQ for processing.",
                        successResults
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occurred during the process
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing request.",
                    errors = new List<ErrorResponse>
                {
                    new ErrorResponse { Message = ex.Message }
                }
                });
            }
        }

        [HttpPost("publish-examdata")]
        public async Task<IActionResult> PublishExamData([FromBody] PublishExamDataRequest request)
        {
            if (request == null || request.Data == null || request.Keys == null || !request.Keys.Any())
            {
                return BadRequest("keys, and Data must be provided.");
            }

            // Filter out duplicate records based on regno and enrollmentno
            var uniqueRecords = new List<ExamRecord>();
            var seenKeys = new HashSet<string>(); // Set to track already seen regno + enrollmentno

            foreach (var row in request.Data)
            {
                var uniqueKey = $"{row.formid}-{row.allocid}"; // Combining regno and enrollmentno to make sure they are unique
                if (!seenKeys.Contains(uniqueKey))
                {
                    uniqueRecords.Add(row);
                    seenKeys.Add(uniqueKey);
                }
            }

            var errors = new List<ErrorResponse>();
            var successResults = new List<SuccessResponse>();

            try
            {
                // Send each record to RabbitMQ for processing in the worker (Node.js)
                foreach (var row in uniqueRecords)
                {
                    var data = new
                    {
                        row.rollno,
                        row.name,
                        row.cycle,
                        row.examType,
                        row.examMonth,
                        row.examYear,
                        row.paper,
                        row.formid,
                        row.allocid,
                        row.marks,
                        Publishtime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    try
                    {
                        // Prepare the message
                        var message = new
                        {
                            streamName = "Result",
                            keys = row.rollno,
                            data = data
                        };

                        // Send the message to RabbitMQ
                        Console.WriteLine(message);
                        await _rabbitMqProducer.SendDataToQueue(message, "ExamQueue");

                        successResults.Add(new SuccessResponse
                        {
                            rollno = row.rollno,
                            Message = $"Successfully sent data for regno {row.rollno} to RabbitMQ.",
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ErrorResponse
                        {
                            rollno = row.rollno,
                            Message = $"Error processing data for regno {row.rollno}: {ex.Message}"
                        });
                    }
                }

                // Return appropriate response based on the results
                if (errors.Any())
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Some records failed to process.",
                        errors,
                        successResults
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = true,
                        message = "All data sent to RabbitMQ for processing.",
                        successResults
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occurred during the process
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing request.",
                    errors = new List<ErrorResponse>
                {
                    new ErrorResponse { Message = ex.Message }
                }
                });
            }
        }

        [HttpPost("toggle-node")]
        public IActionResult ToggleNode([FromBody] bool isNode1Active)
        {
            _multichainService.ToggleNode(isNode1Active);
            return Ok(new { success = true, message = $"Node switched. Node 1 active: {isNode1Active}" });
        }
    }

    public class PublishDataRequest
    {
        // public string StreamName { get; set; }
        public string[] Keys { get; set; }
        public List<DataRecord> Data { get; set; }  // Assuming this is a list of records to be processed
    }

    public class PublishExamDataRequest
    {
        public string[] Keys { get; set; }
        public List<ExamRecord> Data { get; set; }  // Assuming this is a list of records to be processed
    }

    public class FileData
    {
        public string rollno { get; set; }
        public string filehash { get; set; }
        public string filepath { get; set; }
    }

    public class DataRecord
    {
        public string regno { get; set; }
        public string fullname { get; set; }
        public string fname { get; set; }
        public string mname { get; set; }
        public string enrollmentNo { get; set; }
        public string C_Mobile { get; set; }
        public string C_Address { get; set; }
        public string C_Pincode { get; set; }
        public string AdhaarNo { get; set; }
    }

    public class ExamRecord
    {
        public string rollno { get; set; }
        public string name { get; set; }
        public string cycle { get; set; }
        public string examType { get; set; }
        public string examMonth { get; set; }
        public string examYear { get; set; }
        public string paper { get; set; }
        public string formid { get; set; }
        public string allocid { get; set; }

        public string marks { get; set; }
    }



    public class ErrorResponse
    {
        public string regno { get; set; }

        public string rollno { get; set; }
        public string Message { get; set; }

        public string formid { get; set; }

        public string Allocid { get; set; }

    }
    public class SuccessResponse
    {
        public string regno { get; set; }

        public string rollno { get; set; }
        public string Message { get; set; }
        public string Result { get; set; } // Ensure this is of type string
    }

    public class MultiChainResponse
    {
        public bool Success { get; set; }
        public List<MasterDataResult> Result { get; set; }


    }

    public class MultiChainExamMarksResponse
    {
        public bool Success { get; set; }
        public List<ExamMarksDataResult> Result { get; set; }


    }



    public class ChainDataResult
    {
        [JsonProperty("result")]
        public ChainData Result { get; set; }

        [JsonProperty("error")]
        public object Error { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class ChainData
    {
        [JsonProperty("blocks")]
        public int Blocks { get; set; }

        // [JsonProperty("rewards")]
        // public int Rewards { get; set; }

        // [JsonProperty("addresses")]
        // public object Addresses { get; set; }

        // [JsonProperty("transactions")]
        // public object Transactions { get; set; }

        [JsonProperty("assets")]
        public int Assets { get; set; }

        [JsonProperty("streams")]
        public int Streams { get; set; }

        // [JsonProperty("filters")]
        // public int Filters { get; set; }

        // [JsonProperty("variables")]
        // public int Variables { get; set; }

        // [JsonProperty("libraries")]
        // public int Libraries { get; set; }

        // [JsonProperty("upgrades")]
        // public int Upgrades { get; set; }

        // [JsonProperty("licenses")]
        // public int Licenses { get; set; }
    }

    public class MultiChain
    {
        public bool Success { get; set; }

        public int Result { get; set; }
    }

    public class MultiChainInfo
    {
        public bool Success { get; set; }

        [JsonProperty("connections")]
        public int Connections { get; set; }
    }

    public class MultiResponse
    {
        [JsonProperty("result")]
        public MultiChainInfo Result { get; set; }
    }

    public class MasterDataResult
    {
        public List<string> Publishers { get; set; }
        public List<string> Keys { get; set; }
        public bool Offchain { get; set; }
        public bool Available { get; set; }
        public StudentData Data { get; set; } // Make sure to map the "data" object
        public int Confirmations { get; set; }
        public long Blocktime { get; set; }
        public string Txid { get; set; }

        public int blocks { get; set; }

        public int assets { get; set; }

        public int streams { get; set; }
    }

    public class ExamMarksDataResult
    {
        public List<string> Publishers { get; set; }
        public List<string> Keys { get; set; }
        public bool Offchain { get; set; }
        public bool Available { get; set; }
        public ExamMarksData Data { get; set; } // Make sure to map the "data" object
        public int Confirmations { get; set; }
        public long Blocktime { get; set; }
        public string Txid { get; set; }

        public int blocks { get; set; }

        public int assets { get; set; }

        public int streams { get; set; }
    }


    public class StudentData
    {
        // This will map the "json" property inside the "data" object
        public StudentJson Json { get; set; } // Add this to hold the actual student data
    }

    public class ExamMarksData
    {
        // This will map the "json" property inside the "data" object
        public ExamJson Json { get; set; } // Add this to hold the actual student data
    }

    public class StudentJson
    {
        public string Regno { get; set; }
        public string Fullname { get; set; }
        public string Fname { get; set; }
        public string Mname { get; set; }
        public string EnrollmentNo { get; set; }
        public string C_Mobile { get; set; }
        public string C_Address { get; set; }
        public string C_Pincode { get; set; }
        public string AdhaarNo { get; set; }
        public int Version { get; set; }
        public long Publishtime { get; set; }
    }

    public class ExamJson
    {
        public string Rollno { get; set; }
        public string Cycle { get; set; }
        public string ExamType { get; set; }
        public string ExamMonthYear { get; set; }
        public string Paper { get; set; }
        public string Allocid { get; set; }
        public string MaxMarks { get; set; }
        public string MarksObtained { get; set; }
        public string Absent { get; set; }
        public string FeedDate { get; set; }
        public string UMCRemarks { get; set; }
        public string UMCType { get; set; }
        public string OperationType { get; set; }
        public string ModifiedBy { get; set; }

        public string Loginname { get; set; }

        public string ChangeDate { get; set; }
        public long Publishtime { get; set; }
    }







}
