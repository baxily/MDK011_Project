using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using SveshofReff.Models;

namespace SveshofReff.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString = DatabaseInitializer.ConnectionString;

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            return await connection.QueryAsync<User>("SELECT * FROM Users");
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string query)
        {
            using var connection = new SqliteConnection(_connectionString);
            var sql = "SELECT * FROM Users WHERE FullName LIKE @Query OR PhoneNumber LIKE @Query";
            return await connection.QueryAsync<User>(sql, new { Query = $"%{query}%" });
        }

        public async Task<User?> GetUserByReferralCodeAsync(string code)
        {
            using var connection = new SqliteConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE ReferralCode = @Code", new { Code = code });
        }

        public async Task<IEnumerable<User>> GetReferralsAsync(int inviterId)
        {
            using var connection = new SqliteConnection(_connectionString);
            return await connection.QueryAsync<User>(
                "SELECT * FROM Users WHERE InviterID = @InviterId", new { InviterId = inviterId });
        }

        /// <summary>
        /// Добавляет пользователя и начисляет баллы в рамках одной транзакции.
        /// </summary>
        public async Task AddUserAsync(User user, string? inviterCode)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                int? inviterId = null;
                if (!string.IsNullOrWhiteSpace(inviterCode))
                {
                    var inviter = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM Users WHERE ReferralCode = @Code",
                        new { Code = inviterCode }, transaction);

                    if (inviter != null)
                    {
                        inviterId = inviter.ID;
                        // Начисляем бонус пригласившему
                        int inviterBonus = 100;
                        await connection.ExecuteAsync(
                            "UPDATE Users SET PointsBalance = PointsBalance + @Bonus WHERE ID = @Id",
                            new { Bonus = inviterBonus, Id = inviterId }, transaction);

                        await connection.ExecuteAsync(
                            "INSERT INTO Transactions (UserID, OperationType, PointsAmount, Description, TransactionDate) VALUES (@UserId, 'Начисление', @Amount, 'Бонус за приглашение', @Date)",
                            new { UserId = inviterId, Amount = inviterBonus, Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }, transaction);
                        
                        // Приветственный бонус новому пользователю
                        user.PointsBalance = 50; 
                    }
                }

                user.InviterID = inviterId;
                user.RegistrationDate = DateTime.Now;

                var insertUserSql = @"
                    INSERT INTO Users (FullName, PhoneNumber, ReferralCode, InviterID, PointsBalance, RegistrationDate) 
                    VALUES (@FullName, @PhoneNumber, @ReferralCode, @InviterID, @PointsBalance, @RegistrationDate);
                    SELECT last_insert_rowid();";

                var newUserId = await connection.ExecuteScalarAsync<int>(insertUserSql, new
                {
                    user.FullName,
                    user.PhoneNumber,
                    user.ReferralCode,
                    user.InviterID,
                    user.PointsBalance,
                    RegistrationDate = user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss")
                }, transaction);

                if (user.PointsBalance > 0)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO Transactions (UserID, OperationType, PointsAmount, Description, TransactionDate) VALUES (@UserId, 'Начисление', @Amount, 'Приветственный бонус', @Date)",
                        new { UserId = newUserId, Amount = user.PointsBalance, Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task DeleteUserAsync(int userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync("DELETE FROM Transactions WHERE UserID = @UserId", new { UserId = userId }, transaction);
                await connection.ExecuteAsync("UPDATE Users SET InviterID = NULL WHERE InviterID = @UserId", new { UserId = userId }, transaction);
                await connection.ExecuteAsync("DELETE FROM Users WHERE ID = @UserId", new { UserId = userId }, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
