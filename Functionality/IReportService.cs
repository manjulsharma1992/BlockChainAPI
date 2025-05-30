using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MultiChainAPI.Models;

namespace MultiChainAPI.Functionality
{
    public interface IReportService
    {


    Task<bool> CheckStudentEligibility(int degreeId, int cycleId, int subjectId, string userId);
    Task<bool> CheckResultDeclared(string rollNo, int cycleId, int examTypeId, int examConfigId, int subjectId, string userId);

    Task<string> GenerateReportAsync(ReportRequestModel model);
    Task<DataSet> GetExamReportDataAsync(ReportRequestModel model);
    Task<byte[]> GenerateExamReportAsync(DataSet ds);
    //  byte[] GenerateExamReport(DataSet ds);


    //   DataSet GetExamReportData(ReportRequestModel model);
   
        
    }
}