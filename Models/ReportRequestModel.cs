using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiChainAPI.Models
{
   public class ReportRequestModel
{
    public string? RollNo { get; set; }  // Changed to long for large numbers
    public int? FromRollNo { get; set; }  // Nullable int to allow null values
    public int? ToRollNo { get; set; }  // Nullable int to allow null values
    public int CycleId { get; set; }
    public int ExamTypeId { get; set; }
    public int ExamConfigId { get; set; }
    public int DegreeId { get; set; }
    public int SessionId { get; set; }
    public int SubjectId { get; set; }
    public int BatchId { get; set; }
    public int StudentCategoryId { get; set; } = 0;  // Default value
    public int IsDMCWrite { get; set; } = 0;  // Default value
    public string CollegeXml { get; set; }
    public string UserId { get; set; }
}

}