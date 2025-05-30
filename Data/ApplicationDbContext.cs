using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models;
using MultiChainAPI.Models;

namespace MultiChainAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
  {

    
    
  }

        // Define DbSets for the tables you want to access
         public DbSet<Student> TestStudents { get; set; }

         public DbSet<Employee> Employees {get; set;}

        // Override OnModelCreating if needed
        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     // Your model configuration
        //     base.OnModelCreating(modelBuilder);
        // }
    }
}
