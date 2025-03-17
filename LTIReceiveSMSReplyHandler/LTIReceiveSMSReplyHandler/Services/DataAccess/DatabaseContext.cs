using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using LTIReceiveSMSReplyHandler.Services.DataAccess;
using Microsoft.VisualBasic;

namespace LTIReceiveSMSReplyHandler.Services.DataAccess
{
    public class DatabaseContext : DbContext
    {
        private readonly string _connectionString;

        public DatabaseContext(DbContextOptions<DatabaseContext> options, IConfiguration configuration)
            : base(options)
        {
            _connectionString = configuration["Databases.Test_Customer_Portal"];
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("[DatabaseContext] Connection string is NULL at construction.");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("[DatabaseContext] OnConfiguring - Connection string is NULL.");
                }
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }
    }
}
