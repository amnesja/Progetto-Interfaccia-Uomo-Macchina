using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ordo.Services.Design
{
    public class OrdoDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrdoDbContext>
    {
        public OrdoDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<OrdoDbContext>();

            // Usa la variabile d'ambiente ORDO_CONNECTION se presente, altrimenti fallback a LocalDB.
            var connectionString = Environment.GetEnvironmentVariable("ORDO_CONNECTION")
                                   ?? "Server=(localdb)\\mssqllocaldb;Database=Ordo;Trusted_Connection=True;MultipleActiveResultSets=true";

            // Sceglie il provider in base alla connection string (SQLite vs SQL Server)
            if (!string.IsNullOrEmpty(connectionString) &&
                (connectionString.Contains("Data Source=") || connectionString.Contains("Filename=") || connectionString.EndsWith(".db")))
            {
                builder.UseSqlite(connectionString);
            }
            else
            {
                builder.UseSqlServer(connectionString);
            }

            return new OrdoDbContext(builder.Options);
        }
    }
}