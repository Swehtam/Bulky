using System.Linq.Expressions;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        //T - Generic
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includedProperties = null);
        T Get(Expression<Func<T, bool>> filter, string? includedProperties = null, bool tracked = false);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
