using Bulky.Models;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IApplicationUserRepository : IRepository<ApplicationUser>
    {
        void UpdateCompany(string userID, int? companyID);

        void LockUser(string userID);

        void UnlockUser(string userID);
    }
}

