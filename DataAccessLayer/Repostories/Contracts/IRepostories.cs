using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repostories.Contracts
{
   
        public interface IRepository<T> where T : class
        {
            void Add(T entity);
            T GetById(int id);
            List<T> GetAll();
            void Update(T entity);
            void Delete(int id);
            List<T> Search(string keyword);
        }
}
