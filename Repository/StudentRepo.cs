using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Models;
using MultiChainAPI.Data;

namespace MultiChainAPI.Repository
{
    public class StudentRepo : IStudent
    {

        private readonly ApplicationDbContext _context;

        public StudentRepo(ApplicationDbContext context)
        {

            _context=context;
            
        }


       public IEnumerable<Student> GetAllStudents()
        {
           return _context.TestStudents;
        }

       public Student GetStudentbyid(int id)
        {
            return new Student { ID = id,  fullname = "John Doe" };
        }
    }
}