using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using SveshofReff.Models;

namespace SveshofReff.Data
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly string _connectionString = DatabaseInitializer.ConnectionString;

        public async Task<IEnumerable<Transaction>> GetUserTransactionsAsync(int userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            return await connection.QueryAsync<Transaction>(
                "SELECT * FROM Transactions WHERE UserID = @UserId ORDER BY TransactionDate DESC", 
                new { UserId = userId });
        }
    }
}
