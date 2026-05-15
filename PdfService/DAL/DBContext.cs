using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
namespace PdfService.DAL
{
    public class AppDBContext : DbContext
    {
        public DbSet<Pdf> Pdfs { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Pdf>()
                .Property(p => p.Id)
                .HasDefaultValueSql("gen_random_uuid()");
        }

        public async Task<DbDataReader> GetPdfTextReader(Guid id)
        {
            var connection = Database.GetDbConnection();
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT \"Text\" FROM \"Pdfs\" WHERE \"Id\" = @id";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@id";
            parameter.Value = id;
            command.Parameters.Add(parameter);

            return await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection);
        }
    }
}
