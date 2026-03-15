using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace proyecto.Data
{
    public class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=CHAR\\SQLEXPRESS;Database=proyectoDb;Trusted_Connection=True;TrustServerCertificate=True;"
            );

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}