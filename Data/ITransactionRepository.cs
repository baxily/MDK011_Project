using System.Collections.Generic;
using System.Threading.Tasks;
using SveshofReff.Models;

namespace SveshofReff.Data
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetUserTransactionsAsync(int userId);
    }
}
