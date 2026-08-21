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

            // Legge ORDO_CONNECTION oppure usa SQLite locale come fallback (standard del team)
            var connectionString = Environment.GetEnvironmentVariable("ORDO_CONNECTION")
                                   ?? "Data Source=Ordo.db";

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