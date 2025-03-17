using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using LTIReceiveSMSReplyHandler.Services.DataAccess;
using Microsoft.VisualBasic;

namespace LTIReceiveSMSReplyHandler.Services.DataAccess
{
    public class DatabaseServices
    {
        private readonly DatabaseContext _dbContext;
        private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

        public DatabaseServices(DatabaseContext dbContext, IDbContextFactory<DatabaseContext> dbContextFactory)
        {
            _dbContext = dbContext;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<string?> ValidateUserThroughPhone(string phone)
        {
            SqlParameter pPhone = new SqlParameter("@Phone", SqlDbType.NVarChar, 20) { Value = phone };

            using var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "EXEC dbo.CheckUserExistsForNotificationWithPhone @Phone";
            command.CommandType = CommandType.Text;
            command.Parameters.Add(pPhone);

            using var reader = await command.ExecuteReaderAsync();
            return reader.Read() && !reader.IsDBNull(0) ? reader.GetString(0) : null;
        }

        public async Task<bool> OptUserOutFromSMS(string email, string phone)
        {
            SqlParameter pEmail = new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = email };
            SqlParameter pPhone = new SqlParameter("@Phone", SqlDbType.NVarChar, 20) { Value = phone };

            int rowsAffected = await _dbContext.Database.ExecuteSqlRawAsync(
                "EXEC dbo.OptUserOutOfSMS @Email, @Phone",
                pEmail, pPhone
            );

            return rowsAffected > 0;
        }

        public async Task<bool> CheckOptInStatus(string email)
        {
            SqlParameter pEmail = new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = email };

            using var connection = _dbContext.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "EXEC dbo.CheckOptInStatus @Email";
            command.CommandType = CommandType.Text;
            command.Parameters.Add(pEmail);

            using var reader = command.ExecuteReader();
            return reader.Read() && !reader.IsDBNull(0) && reader.GetBoolean(0);
        }

        public async Task LogNotificationEvent(
            string username,
            string fromPhoneNumber,
            string userPreference,
            string type,
            string? notificationType,
            string message,
            string? notes,
            string? coNum)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC dbo.LTI_Notification_Log @Username, @FromPhoneNumber, @UserPreference, @Type, @NotificationType, @Message, @Notes, @CoNum",
                new SqlParameter("@Username", SqlDbType.NVarChar, 255) { Value = username },
                new SqlParameter("@FromPhoneNumber", SqlDbType.NVarChar, 20) { Value = fromPhoneNumber },
                new SqlParameter("@UserPreference", SqlDbType.NVarChar, 50) { Value = userPreference },
                new SqlParameter("@Type", SqlDbType.NVarChar, 50) { Value = type },
                new SqlParameter("@NotificationType", SqlDbType.NVarChar, 50) { Value = (object?)notificationType ?? DBNull.Value },
                new SqlParameter("@Message", SqlDbType.NVarChar, -1) { Value = (object?)message ?? DBNull.Value },
                new SqlParameter("@Notes", SqlDbType.NVarChar, -1) { Value = (object?)notes ?? DBNull.Value },
                new SqlParameter("@CoNum", SqlDbType.NVarChar, 50) { Value = (object?)coNum ?? DBNull.Value }
            );
        }
    }
}
