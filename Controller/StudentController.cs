using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Models;
using MultiChainAPI.Repository;

namespace MultiChainAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {

        private readonly IStudent _studentservice;

        public StudentController(IStudent studentservice)
        {
            _studentservice = studentservice;
        }

        [HttpGet("{getbyid}")]
        public ActionResult<Student> Get(int id)
        {
            var student = _studentservice.GetStudentbyid(id);
            if (student == null)
                return NotFound();

            return Ok(student);
        }

        [HttpGet("{GetSum}")]
        public int Addnumbers([FromQuery] int a, [FromQuery] int b)
        {

            
            return a + b;
            
        }

    }
}