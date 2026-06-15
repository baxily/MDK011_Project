using System.Collections.Generic;
using System.Threading.Tasks;
using SveshofReff.Models;

namespace SveshofReff.Data
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> SearchUsersAsync(string query);
        Task<User?> GetUserByReferralCodeAsync(string code);
        Task<IEnumerable<User>> GetReferralsAsync(int inviterId);
        Task AddUserAsync(User user, string? inviterCode);
        Task DeleteUserAsync(int userId);
    }
}
