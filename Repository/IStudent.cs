using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models;

namespace MultiChainAPI.Repository
{
    public interface IStudent
    {
    public IEnumerable<Student> GetAllStudents();
    public Student GetStudentbyid(int id);
    }
}