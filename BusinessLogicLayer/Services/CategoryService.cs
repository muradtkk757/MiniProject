using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Repostories;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly CategoryRepository _repo;

        public CategoryService(CategoryRepository repo)
        {
            _repo = repo;
        }

        public void Create(Category category)
        {
            Validate(category, true);
            _repo.Add(category);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }

        public Category Get(int id)
        {
            return _repo.GetById(id);
        }

        public List<Category> GetAll()
        {
            return _repo.GetAll();
        }

        public List<Category> Search(string keyword)
        {
            return _repo.Search(keyword);
        }

        public void Update(Category category)
        {
            Validate(category, false);
            _repo.Update(category);
        }

        private void Validate(Category c, bool isNew)
        {
            if (c == null) throw new ArgumentException("Category cannot be null.");
            if (string.IsNullOrWhiteSpace(c.Name)) throw new ArgumentException("Category name is required.");
        }
    }
}
