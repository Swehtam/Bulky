using System.Globalization;
using System.Linq.Expressions;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Bulky.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> _dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            _dbSet = _db.Set<T>();
            //_db.Category == _dbSet
            _db.Products.Include(u => u.Category);
        }

        public void Add(T entity) => _dbSet.Add(entity);

        public T Get(Expression<Func<T, bool>> filter, string? includedProperties = null, bool tracked = false)
        {
            IQueryable<T> query = tracked ? _dbSet : _dbSet.AsNoTracking();
            query = query.Where(filter);
            if (!string.IsNullOrEmpty(includedProperties))
            {
                foreach (var includePro in includedProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includePro);
                }
            }
            return query.FirstOrDefault();
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string ? includedProperties = null)
        {
            IQueryable<T> query = _dbSet;
            
            if(filter != null) query = query.Where(filter);

            if (!string.IsNullOrEmpty(includedProperties))
            {
                foreach(var includePro in includedProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includePro);
                }
            }
            return query.ToList();
        }

        public void Remove(T entity) => _dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);
    }
}
