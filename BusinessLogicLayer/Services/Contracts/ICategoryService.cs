using DataAccessLayer.Repostories.Contracts;
using DataAccessLayerModels;

namespace BusinessLogicLayer.Services.Contracts
{
        public interface ICategoryService
        {
            void Create(Category category);
            Category Get(int id);
            List<Category> GetAll();
            void Update(Category category);
            void Delete(int id);
            List<Category> Search(string keyword);
        }
    }