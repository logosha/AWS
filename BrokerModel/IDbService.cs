using System;

namespace Core.DBService
{
    public interface IDbService : IDisposable
    {
        IRepo<TEntity> Repo<TEntity>() where TEntity : class;
        int SaveChanges();
    }

    /*
    * how to use it?
    * available unityContainet object is a must!
    * 
    * using (var dbService = unityContainer.Resolve<IDBService>())
    *  {
    *    IRepo<User> repo = dbService.Repo<User>();
    *    repo.Add(new User() { FirstName = "Ostap", LastName = "Bender" });
    *    IRepo<Order> repo1 = dbService.Repo<Order>();
    *    repo1.Add(new Order() { Name = "Order" });
    *
    *    dbService.SaveChanges();
    *  }
    * 
    */
}
