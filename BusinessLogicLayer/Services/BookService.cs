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
    public class BookService : IBookService
    {
        private readonly BookRepository _repo;
        private readonly CategoryRepository _categoryRepo;

        public BookService(BookRepository repo, CategoryRepository categoryRepo)
        {
            _repo = repo;
            _categoryRepo = categoryRepo;
        }

        public void Create(Book book)
        {
            Validate(book, isNew: true);
            _repo.Add(book);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }

        public Book Get(int id)
        {
            return _repo.GetById(id);
        }

        public List<Book> GetAll()
        {
            return _repo.GetAll();
        }

        public List<Book> Search(string keyword)
        {
            return _repo.Search(keyword);
        }

        public void Update(Book book)
        {
            Validate(book, isNew: false);
            _repo.Update(book);
        }

        private void Validate(Book b, bool isNew)
        {
            if (b == null) throw new ArgumentException("Book cannot be null.");
            if (string.IsNullOrWhiteSpace(b.Title)) throw new ArgumentException("Title is required.");
            if (string.IsNullOrWhiteSpace(b.Author)) throw new ArgumentException("Author is required.");
            if (string.IsNullOrWhiteSpace(b.ISBN) || b.ISBN.Length != 13) throw new ArgumentException("ISBN must be 13 characters.");
            if (b.PublishedYear < 1000 || b.PublishedYear > DateTime.Now.Year) throw new ArgumentException("Published year is not valid.");
            if (_categoryRepo.GetById(b.CategoryId) == null) throw new ArgumentException("Category does not exist.");
        }
    }
}
