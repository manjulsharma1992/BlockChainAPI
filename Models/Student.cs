using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class Student
    {
        public int ID {get; set;}
        public string regno { get; set; }
        public string fullname { get; set; }
        public string fname { get; set; }
        public string mname { get; set; }
        public string enrollmentno { get; set; }
        public string C_Mobile { get; set; }
        public string C_Address { get; set; }
        public string C_Pincode { get; set; }
        public string AdhaarNo { get; set; }

        public string ABCID { get; set; }

        //public IEnumerable<Student> students;
    }
}