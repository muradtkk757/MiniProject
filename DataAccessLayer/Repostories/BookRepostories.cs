using DataAccessLayer.Data;
using DataAccessLayer.Repostories.Contracts;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repostories
{

    public class BookRepository : IRepository<Book>
    {
        private readonly string _file = DataContext.BooksFile;

        private const int ID_LENGTH = 5;
        private const int TITLE_LENGTH = 30;
        private const int AUTHOR_LENGTH = 25;
        private const int ISBN_LENGTH = 13;
        private const int YEAR_LENGTH = 4;
        private const int CATEGORYID_LENGTH = 5;
        private const int ISAVAILABLE_LENGTH = 1;

        public BookRepository()
        {
            DataContext.EnsureDataFiles();
        }

        public void Add(Book entity)
        {
            var all = GetAll();
            int nextId = (all.Any() ? all.Max(b => b.Id) : 0) + 1;
            entity.Id = nextId;
            string line = BuildLine(entity);
            File.AppendAllText(_file, line + Environment.NewLine);
        }

        public void Delete(int id)
        {
            var lines = File.ReadAllLines(_file).ToList();
            int idx = lines.FindIndex(l => ParseIdFromLine(l) == id);
            if (idx >= 0)
            {
                lines.RemoveAt(idx);
                File.WriteAllLines(_file, lines);
            }
            else
            {
                throw new Exception("Book not found.");
            }
        }

        public List<Book> GetAll()
        {
            var result = new List<Book>();
            foreach (var line in File.ReadAllLines(_file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                result.Add(ParseLine(line));
            }
            return result;
        }

        public Book GetById(int id)
        {
            var line = File.ReadAllLines(_file).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && ParseIdFromLine(l) == id);
            if (line == null) return null;
            return ParseLine(line);
        }

        public List<Book> Search(string keyword)
        {
            keyword = (keyword ?? string.Empty).ToLower();
            return GetAll().Where(b =>
                (!string.IsNullOrEmpty(b.Title) && b.Title.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(b.Author) && b.Author.ToLower().Contains(keyword)) ||
                b.CategoryId.ToString() == keyword
            ).ToList();
        }

        public void Update(Book entity)
        {
            var lines = File.ReadAllLines(_file).ToList();
            int idx = lines.FindIndex(l => ParseIdFromLine(l) == entity.Id);
            if (idx >= 0)
            {
                lines[idx] = BuildLine(entity);
                File.WriteAllLines(_file, lines);
            }
            else
            {
                throw new Exception("Book not found.");
            }
        }

        private int ParseIdFromLine(string line)
        {
            var idStr = line.Substring(0, ID_LENGTH);
            if (int.TryParse(idStr, out int id)) return id;
            return 0;
        }

        private Book ParseLine(string line)
        {
            // Using exact indices and lengths
            // 0-5 Id
            // 5-35 Title
            // 35-60 Author
            // 60-73 ISBN
            // 73-77 Year
            // 77-82 CategoryId
            // 82-83 IsAvailable
            try
            {
                int index = 0;
                string idS = line.Substring(index, ID_LENGTH); index += ID_LENGTH;
                string title = line.Substring(index, TITLE_LENGTH); index += TITLE_LENGTH;
                string author = line.Substring(index, AUTHOR_LENGTH); index += AUTHOR_LENGTH;
                string isbn = line.Substring(index, ISBN_LENGTH); index += ISBN_LENGTH;
                string yearS = line.Substring(index, YEAR_LENGTH); index += YEAR_LENGTH;
                string catS = line.Substring(index, CATEGORYID_LENGTH); index += CATEGORYID_LENGTH;
                string availS = line.Substring(index, ISAVAILABLE_LENGTH); // index += ISAVAILABLE_LENGTH;

                return new Book
                {
                    Id = int.TryParse(idS, out var i) ? i : 0,
                    Title = title.Trim(),
                    Author = author.Trim(),
                    ISBN = isbn.Trim(),
                    PublishedYear = int.TryParse(yearS, out var y) ? y : 0,
                    CategoryId = int.TryParse(catS, out var c) ? c : 0,
                    IsAvailable = availS == "1"
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse book line. " + ex.Message);
            }
        }

        private string BuildLine(Book b)
        {
            // Id padded left with zeros to length 5
            // Strings padded right
            // IsAvailable as '1' or '0'
            string id = b.Id.ToString().PadLeft(ID_LENGTH, '0');
            string title = (b.Title ?? string.Empty).PadRight(TITLE_LENGTH).Substring(0, TITLE_LENGTH);
            string author = (b.Author ?? string.Empty).PadRight(AUTHOR_LENGTH).Substring(0, AUTHOR_LENGTH);
            string isbn = (b.ISBN ?? string.Empty).PadRight(ISBN_LENGTH).Substring(0, ISBN_LENGTH);
            string year = b.PublishedYear.ToString().PadLeft(YEAR_LENGTH, '0').Substring(0, YEAR_LENGTH);
            string cat = b.CategoryId.ToString().PadLeft(CATEGORYID_LENGTH, '0').Substring(0, CATEGORYID_LENGTH);
            string avail = b.IsAvailable ? "1" : "0";

            return string.Concat(id, title, author, isbn, year, cat, avail);
        }
    }
}


