using System.Data;
using System.Data.SqlClient;
using MultiChainAPI.Functionality;
using MultiChainAPI.Models;
using Dapper;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using iText.Layout.Properties;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;

public class ReportService : IReportService
{
    private readonly string _connectionString;
private readonly IWebHostEnvironment _env;

       
    public ReportService(IWebHostEnvironment env,IConfiguration configuration)
    {
        _env = env;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<bool> CheckStudentEligibility(int degreeId, int cycleId, int subjectId, string userId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var result = await connection.QueryFirstOrDefaultAsync<string>("ACD_CheckStudentCollege", 
                new { degreeId, cycleId, subjectId, userId }, 
                commandType: CommandType.StoredProcedure);

            return result != "Not Eligible";
        }
    }

    public async Task<bool> CheckResultDeclared(string rollNo, int cycleId, int examTypeId, int examConfigId, int subjectId, string userId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            var result = await connection.QueryFirstOrDefaultAsync<bool>(
                "SP_HPU_CheckResult_Live", 
                new { rollNo, cycleId, examTypeId, examConfigId, subjectId, userId }, 
                commandType: CommandType.StoredProcedure);
            return result;
        }
    }
 public async Task<DataSet> GetExamReportDataAsync(ReportRequestModel model)
    {
         using (var connection = new SqlConnection(_connectionString))
         {
             await connection.OpenAsync();
         var parameters = new DynamicParameters();

        if (string.IsNullOrWhiteSpace(model.RollNo))
            parameters.Add("@rollNo", DBNull.Value, DbType.Int64);
        else
            parameters.Add("@rollNo", Convert.ToInt64(model.RollNo), DbType.Int64);
        parameters.Add("@fromrollno", model.FromRollNo ?? (object)DBNull.Value, DbType.Int32);
        parameters.Add("@torolno", model.ToRollNo ?? (object)DBNull.Value, DbType.Int32);
        parameters.Add("@fk_cycleid", model.CycleId, DbType.Int32);
        parameters.Add("@fk_Examtypeid", model.ExamTypeId, DbType.Int32);
        parameters.Add("@fk_examconfig", model.ExamConfigId, DbType.Int32);
        parameters.Add("@fk_degreeid", model.DegreeId, DbType.Int32);
        parameters.Add("@sessionid", model.SessionId, DbType.Int32);
        parameters.Add("@fk_subjectid", model.SubjectId, DbType.Int32);
        parameters.Add("@fk_batchid", model.BatchId, DbType.Int32);
        parameters.Add("@fk_stucatid", model.StudentCategoryId, DbType.Int32);
        parameters.Add("@IsDMCWrite", model.IsDMCWrite, DbType.Int32);
        parameters.Add("@CollegXml", model.CollegeXml, DbType.String);
        parameters.Add("@UserId", model.UserId, DbType.String);

        // Execute stored procedure
        var dataset = new DataSet();
        using (var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(
                "SMS_SP_TR_PGCBCS",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 3000 // Increased timeout
            )))
        {
            do
            {
                var table = new DataTable();
                table.Load(reader);
                dataset.Tables.Add(table);
            } while (!reader.IsClosed);
        }
        //   using (var reader =await  connection.ExecuteReaderAsync("SMS_SP_TR_PGCBCS", parameters, commandType: CommandType.StoredProcedure))
        //     {
        //         do
        //         {
        //             var table = new DataTable();
        //             table.Load(reader);
        //             dataset.Tables.Add(table);
        //         } while (!reader.IsClosed);
        //     }

        if (dataset.Tables.Count == 0 || dataset.Tables[0].Rows.Count == 0)
            return null;

        // Generate PDF from dataset


            return dataset;
        }
    
    }
    
   public async Task<string> GenerateReportAsync(ReportRequestModel model)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();

        var parameters = new DynamicParameters();
        parameters.Add("@rollno", model.RollNo, DbType.Int64);  // Changed to Int64 (long)
        parameters.Add("@fromrollno", model.FromRollNo ?? (object)DBNull.Value, DbType.Int32);
        parameters.Add("@torolno", model.ToRollNo ?? (object)DBNull.Value, DbType.Int32);
        parameters.Add("@fk_cycleid", model.CycleId, DbType.Int32);
        parameters.Add("@fk_Examtypeid", model.ExamTypeId, DbType.Int32);
        parameters.Add("@fk_examconfig", model.ExamConfigId, DbType.Int32);
        parameters.Add("@fk_degreeid", model.DegreeId, DbType.Int32);
        parameters.Add("@sessionid", model.SessionId, DbType.Int32);
        parameters.Add("@fk_subjectid", model.SubjectId, DbType.Int32);
        parameters.Add("@fk_batchid", model.BatchId, DbType.Int32);
        parameters.Add("@fk_stucatid", model.StudentCategoryId, DbType.Int32);
        parameters.Add("@IsDMCWrite", model.IsDMCWrite, DbType.Int32);
        parameters.Add("@CollegXml", model.CollegeXml, DbType.String);
        parameters.Add("@UserId", model.UserId, DbType.String);

        // Execute stored procedure
        var dataset = new DataSet();
        using (var reader = await connection.ExecuteReaderAsync("SMS_SP_TR_PGCBCS", parameters, commandType: CommandType.StoredProcedure))
        {
            do
            {
                var table = new DataTable();
                table.Load(reader);
                dataset.Tables.Add(table);
            } while (!reader.IsClosed);
        }

        if (dataset.Tables.Count == 0 || dataset.Tables[0].Rows.Count == 0)
            return null;

        // Generate PDF from dataset

        return  GeneratePdfTRAnnual(dataset);
      
      
    }
}


private async Task<string> GeneratePdfAsync(DataSet dataset)
{
    var fileName = $"Report_{DateTime.UtcNow.Ticks}.pdf";
    var folderPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Reports");

    if (!Directory.Exists(folderPath))
        Directory.CreateDirectory(folderPath);

    var filePath = System.IO.Path.Combine(folderPath, fileName);

    using (var writer = new PdfWriter(filePath))
    {
        using (var pdf = new PdfDocument(writer))
        {
            var document = new Document(pdf);

            // Title
            document.Add(new Paragraph(new Text("Himachal Pradesh University")).SetFontSize(16));
            document.Add(new Paragraph(new Text("NAAC Accredited 'A' Grade University")).SetFontSize(12));
            document.Add(new Paragraph($"Generated on: {DateTime.Now}").SetFontSize(10));

            if (dataset != null && dataset.Tables.Count > 0)
            {
                Table table = new Table(dataset.Tables[0].Columns.Count);

                // Add Headers
                foreach (DataColumn column in dataset.Tables[0].Columns)
                {
                    table.AddHeaderCell(new Cell().Add(new Paragraph(new Text(column.ColumnName))));
                }

                // Add Rows
                foreach (DataRow row in dataset.Tables[0].Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(item?.ToString() ?? "")));
                    }
                }

                document.Add(table);
            }
        }
    }

    return filePath;
}

public string GeneratePdfTRAnnual(DataSet ds)
        {
            int rowcount = 0;
            try
            {
                 var fileName = $"Report_{DateTime.UtcNow.Ticks}.pdf";
                 var folderPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Reports");

    if (!Directory.Exists(folderPath))
        Directory.CreateDirectory(folderPath);

    var filePath = System.IO.Path.Combine(folderPath, fileName);

                // Store exam type and current date (replace ViewState with a service or local variable)
                string examType = "All Exam Type";
                string currentDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss tt");

                // Define document dimensions and output
                var envelope = new Rectangle(940, 865);
                using var output = new MemoryStream();
                using var writer = new PdfWriter(output);
                using var pdf = new PdfDocument(writer);
                var document = new Document(pdf, new PageSize(envelope), false);
                document.SetMargins(20, 20, 25, 25);

                // Add page event (custom header/footer logic would go here if needed)
                //writer.SetPageEvent(new CustomPageEventHandler());

                // Fonts (iText 7 uses a simpler font system; Arial is default in many cases)
                var fontNormal = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
                var fontBold = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                // Header table
                Table tblHeading = new Table(UnitValue.CreatePercentArray(new float[] { 2f, 60f, 20f })).UseAllAvailableWidth();
                //string logoPath = System.IO.Path.Combine(_env.WebRootPath, "Images/hpu-logo.jpg");
                //Image logo = new Image(ImageDataFactory.Create(logoPath)).ScaleToFit(50f, 60f);
                //Cell logoCell = new Cell(4, 1).Add(logo).SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                   // .SetHorizontalAlignment(HorizontalAlignment.LEFT)
                   // .SetPaddingTop(2f).SetPaddingBottom(2f);
              //  tblHeading.AddCell(logoCell);

                Cell headerCell = new Cell(1, 2).Add(new Paragraph("Himachal Pradesh University").SetFont(fontBold).SetFontSize(14))
                    .SetTextAlignment(TextAlignment.CENTER).SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPaddingTop(2f).SetPaddingBottom(2f);
                tblHeading.AddCell(headerCell);

                headerCell = new Cell(1, 2).Add(new Paragraph("NAAC Accredited 'A' Grade University").SetFont(fontBold).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.CENTER).SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPaddingTop(2f).SetPaddingBottom(5f).SetPaddingLeft(6f);
                tblHeading.AddCell(headerCell);

               // string yearText = _words[Convert.ToInt32(ds.Tables[0].Rows[0]["fk_dyearid"].ToString())]; // Assume _words is defined elsewhere
                headerCell = new Cell(1, 2).Add(new Paragraph($"Tabulation Sheet in respect of {2016} Year Examination held in {ds.Tables[0].Rows[0]["ExamMonthYear"]}. The Result of the student has been shown against their names and roll no.")
                    .SetFont(fontBold).SetFontSize(10)).SetTextAlignment(TextAlignment.CENTER)
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPaddingTop(2f).SetPaddingBottom(8f);
                tblHeading.AddCell(headerCell);

                headerCell = new Cell(1, 2).Add(new Paragraph("").SetFont(fontBold).SetFontSize(11))
                    .SetTextAlignment(TextAlignment.CENTER).SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPaddingTop(2f).SetPaddingBottom(5f);
                tblHeading.AddCell(headerCell);

                document.Add(tblHeading);

                // Abbreviations table
                Table tblAbbreviations = new Table(UnitValue.CreatePercentArray(new float[] { 25f, 25f, 25f, 25f })).UseAllAvailableWidth();
                tblAbbreviations.AddCell(new Cell(1, 4).Add(new Paragraph("Abbreviations").SetFont(fontBold).SetFontSize(14))
                    .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPaddingTop(2f).SetPaddingBottom(6f).SetBorder(new iText.Layout.Borders.SolidBorder(1)));

                string[] abbreviations = {
                    "TE - Term End Examination", "GP - Grade Point", "M.O. - Marks Obtained", "M* - Marks with Grace",
                    "CCA - Continuous Comprehensive Assessment", "CP - Credit Point(Credit X Grade)", "M.P.M - Minimum Passing Marks", "* - CCA not entered",
                    "GL - Grade Letter", "GR - Grace", "M.M. - Maximum Marks", "** - TE not entered",
                    "CR - Credit Earned", "UMC/U - Unfair Means Case", "M\" - Reval Marks", "TMM - Total Max Marks",
                    "COMP. - Compartment", "LCC - Late College Capacity", "", ""
                };

                foreach (var abbr in abbreviations)
                {
                    var cell = new Cell().Add(new Paragraph(abbr).SetFont(fontNormal).SetFontSize(10))
                        .SetTextAlignment(TextAlignment.LEFT).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetPaddingTop(2f).SetBorderLeft(new iText.Layout.Borders.SolidBorder(1))
                        .SetBorderRight(abbr == abbreviations[3] || abbr == abbreviations[7] || abbr == abbreviations[11] || abbr == abbreviations[15] || abbr == abbreviations[19] ? new iText.Layout.Borders.SolidBorder(1) : iText.Layout.Borders.Border.NO_BORDER)
                        .SetBorderTop(iText.Layout.Borders.Border.NO_BORDER)
                        .SetBorderBottom(abbr.Contains("LCC") ? new iText.Layout.Borders.SolidBorder(1) : iText.Layout.Borders.Border.NO_BORDER);
                    if (abbr.Contains("LCC")) cell.SetPaddingBottom(4f);
                    tblAbbreviations.AddCell(cell);
                }

                document.Add(tblAbbreviations);

                // Add more sections (Grading Table, Data Table, etc.) similarly, adapting to iText 7 syntax

                // Finalize document
                document.Close();

                // Handle output (e.g., save to file or return as response)
                byte[] pdfBytes = output.ToArray();
                // Example: Save to file
                //string filePath = System.IO.Path.Combine(_env.WebRootPath, "output.pdf");
                File.WriteAllBytes(filePath, pdfBytes);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error at row {rowcount}: {ex.Message}");
                return "";
            }
        }

  public async Task<byte[]> GenerateExamReportAsync(DataSet ds)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            PdfWriter writer = new PdfWriter(memoryStream);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // **University Header**
            PdfFont titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            document.Add(new Paragraph("Himachal Pradesh University")
                .SetFont(titleFont)
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("NAAC Accredited 'A' Grade University")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Tabulation Sheet in respect of First Semester Examination")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            // **Exam Details**
            DataRow examInfo = ds.Tables[0].Rows[0]; // Assuming the first table contains exam info
            DataRow exam1Info = ds.Tables[1].Rows[0]; // Assuming the first table contains exam info
            document.Add(new Paragraph($"Exam held in {examInfo["ExamMonthYear"]}")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Course: {examInfo["DegreeDisplayname"]} | Semester: {exam1Info["remarks"]} | Exam Year: {exam1Info["ExamMonthYear"]}")
                .SetFontSize(10));

            document.Add(new Paragraph($"College Name: {examInfo["College_Name"]} | Date of Result Declaration: {examInfo["ResultDeclarationdate"]}")
                .SetFontSize(10));

            document.Add(new Paragraph("\n"));

            // **Student Details**
            Table studentTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 4 })).UseAllAvailableWidth();
            studentTable.AddCell(new Cell().Add(new Paragraph("UNIV ROLL NO:")));
            studentTable.AddCell(new Cell().Add(new Paragraph(examInfo["rollNo"].ToString())));
            studentTable.AddCell(new Cell().Add(new Paragraph("CANDIDATE NAME:")));
            studentTable.AddCell(new Cell().Add(new Paragraph(examInfo["fullname"].ToString())));
            studentTable.AddCell(new Cell().Add(new Paragraph("FATHER'S NAME:")));
            studentTable.AddCell(new Cell().Add(new Paragraph(examInfo["fname"].ToString())));
            studentTable.AddCell(new Cell().Add(new Paragraph("MOTHER'S NAME:")));
            studentTable.AddCell(new Cell().Add(new Paragraph(examInfo["mname"].ToString())));
            studentTable.AddCell(new Cell().Add(new Paragraph("Exam Type:")));
            studentTable.AddCell(new Cell().Add(new Paragraph(exam1Info["examtype1"].ToString())));

            document.Add(studentTable);
            document.Add(new Paragraph("\n"));

            // **Subjects & Marks Table**
            Table marksTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1, 1, 1, 1, 1, 1, 1 }))
                .UseAllAvailableWidth();

            // **Header Row**
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("Paper Name")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("IA")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("TE")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("PR")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("GR")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("Total")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("Grade")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("SGPA")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
            marksTable.AddHeaderCell(new Cell().Add(new Paragraph("Status")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

            // **Populate Table with Data**
            foreach (DataRow row in ds.Tables[1].Rows) // Assuming second table contains paper details
            {
                marksTable.AddCell(new Cell().Add(new Paragraph(row["Coursename"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["internal_componentmarks"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["theory_componentmarks"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["practical_componentmarks"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["gracemarks"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["totalmarkspaperwisetobeshown"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["totalmarkspaperwisetobeshown"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["SGPA"].ToString())));
                marksTable.AddCell(new Cell().Add(new Paragraph(row["YearResult"].ToString())));
            }

            document.Add(marksTable);

            // **Footer Section**
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph($"Report Print Date: {DateTime.Now} | Downloaded by: Admin")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Close();
            return memoryStream.ToArray();
        }
    }

    

   
}
