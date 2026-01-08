using DataAccessLayer.Data;
using DataAccessLayer.Repostories.Contracts;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataAccessLayer.Repostories
{
    public class BookRepository : IRepository<Book>
    {
        private readonly string _file = DataAccessLayer.Data.Data.BooksFile;

       
        private const int ID_LENGTH = 5;
        private const int TITLE_LENGTH = 30;
        private const int AUTHOR_LENGTH = 25;
        private const int ISBN_LENGTH = 13;
        private const int YEAR_LENGTH = 4;
        private const int CATEGORYID_LENGTH = 5;
        private const int ISAVAILABLE_LENGTH = 1;
        private const int MEMBERID_LENGTH = 5; 

        public BookRepository()
        {
            DataAccessLayer.Data.Data.EnsureDataFiles();
        }

        public void Add(Book entity)
        {
            var allBooks = GetAll();
            int nextId = (allBooks.Any() ? allBooks.Max(b => b.Id) : 0) + 1;
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
        }

        public List<Book> GetAll()
        {
            var result = new List<Book>();
            if (!File.Exists(_file)) return result;

            foreach (var line in File.ReadAllLines(_file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var b = ParseLine(line);
                if (b != null) result.Add(b);
            }
            return result;
        }

        public Book GetById(int id)
        {
            if (!File.Exists(_file)) return null;
            var line = File.ReadAllLines(_file)
                           .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && ParseIdFromLine(l) == id);
            return line == null ? null : ParseLine(line);
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
        }

        public List<Book> Search(string keyword)
        {
          
            return GetAll().Where(x => x.Title.Contains(keyword)).ToList();
        }

       
        private int ParseIdFromLine(string line)
        {
            if (line.Length < ID_LENGTH) return 0;
            var idStr = line.Substring(0, ID_LENGTH);
            return int.TryParse(idStr, out int id) ? id : 0;
        }

        private Book ParseLine(string line)
        {
            try
            {
                int index = 0;
                
                string idS = line.Substring(index, ID_LENGTH); index += ID_LENGTH;
                string title = line.Substring(index, TITLE_LENGTH); index += TITLE_LENGTH;
                string author = line.Substring(index, AUTHOR_LENGTH); index += AUTHOR_LENGTH;
                string isbn = line.Substring(index, ISBN_LENGTH); index += ISBN_LENGTH;
                string yearS = line.Substring(index, YEAR_LENGTH); index += YEAR_LENGTH;
                string catS = line.Substring(index, CATEGORYID_LENGTH); index += CATEGORYID_LENGTH;
                string availS = line.Substring(index, ISAVAILABLE_LENGTH); index += ISAVAILABLE_LENGTH;

              
                string memIdS = "0";
                if (line.Length >= index + MEMBERID_LENGTH)
                {
                    memIdS = line.Substring(index, MEMBERID_LENGTH);
                }

                return new Book
                {
                    Id = int.TryParse(idS, out var i) ? i : 0,
                    Title = title.Trim(),
                    Author = author.Trim(),
                    ISBN = isbn.Trim(),
                    PublishedYear = int.TryParse(yearS, out var y) ? y : 0,
                    CategoryId = int.TryParse(catS, out var c) ? c : 0,
                    IsAvailable = availS == "1",
                    CurrentMemberId = int.TryParse(memIdS, out var m) ? m : 0
                };
            }
            catch
            {
                return null;
            }
        }

        private string BuildLine(Book b)
        {
            string id = b.Id.ToString().PadLeft(ID_LENGTH, '0');
            string title = (b.Title ?? "").PadRight(TITLE_LENGTH).Substring(0, TITLE_LENGTH); 
            string author = (b.Author ?? "").PadRight(AUTHOR_LENGTH).Substring(0, AUTHOR_LENGTH);
            string isbn = (b.ISBN ?? "").PadRight(ISBN_LENGTH).Substring(0, ISBN_LENGTH);
            string year = b.PublishedYear.ToString().PadLeft(YEAR_LENGTH, '0');
            if (year.Length > YEAR_LENGTH) year = year.Substring(0, YEAR_LENGTH);
            string cat = b.CategoryId.ToString().PadLeft(CATEGORYID_LENGTH, '0');
            string avail = b.IsAvailable ? "1" : "0";

          
            string memId = b.CurrentMemberId.ToString().PadLeft(MEMBERID_LENGTH, '0');

            return string.Concat(id, title, author, isbn, year, cat, avail, memId);
        }
    }
}