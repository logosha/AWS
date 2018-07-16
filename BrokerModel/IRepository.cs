using System;
using System.Collections.Generic;

namespace Core.DBService
{
    public interface IRepo<TEntity> where TEntity : class
    {
        IEnumerable<TEntity> GetAll();
        IEnumerable<TEntity> FindBy(Func<TEntity, bool> predicate);
        void Add(TEntity entity);
        void Remove(TEntity entity);
    }
}
