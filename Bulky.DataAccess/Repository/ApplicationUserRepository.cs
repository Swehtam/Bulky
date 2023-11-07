using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        private ApplicationDbContext _db;

        public ApplicationUserRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void LockUser(string userID)
        {
            var userObj = _db.ApplicationUsers.FirstOrDefault(p => p.Id == userID);
            userObj.LockoutEnd = DateTime.Now.AddYears(1000);
        }

        public void UnlockUser(string userID)
        {
            var userObj = _db.ApplicationUsers.FirstOrDefault(p => p.Id == userID);
            userObj.LockoutEnd = DateTime.Now;
        }

        public void UpdateCompany(string userID, int? companyID)
        {
            var userObj = _db.ApplicationUsers.FirstOrDefault(p => p.Id == userID);
            userObj.CompanyId = companyID;
        }
    }
}
