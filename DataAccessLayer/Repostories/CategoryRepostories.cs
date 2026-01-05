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
    public class CategoryRepository : IRepository<Category>
    {
        private readonly string _file = DataContext.CategoriesFile;

        private const int ID_LENGTH = 5;
        private const int NAME_LENGTH = 30;
        private const int DESCRIPTION_LENGTH = 50;

        public CategoryRepository()
        {
            DataContext.EnsureDataFiles();
        }

        public void Add(Category entity)
        {
            var all = GetAll();
            int nextId = (all.Any() ? all.Max(c => c.Id) : 0) + 1;
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
                throw new Exception("Category not found.");
        }

        public List<Category> GetAll()
        {
            var result = new List<Category>();
            foreach (var line in File.ReadAllLines(_file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                result.Add(ParseLine(line));
            }
            return result;
        }

        public Category GetById(int id)
        {
            var line = File.ReadAllLines(_file).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && ParseIdFromLine(l) == id);
            if (line == null) return null;
            return ParseLine(line);
        }

        public List<Category> Search(string keyword)
        {
            keyword = (keyword ?? string.Empty).ToLower();
            return GetAll().Where(c => (!string.IsNullOrEmpty(c.Name) && c.Name.ToLower().Contains(keyword))).ToList();
        }

        public void Update(Category entity)
        {
            var lines = File.ReadAllLines(_file).ToList();
            int idx = lines.FindIndex(l => ParseIdFromLine(l) == entity.Id);
            if (idx >= 0)
            {
                lines[idx] = BuildLine(entity);
                File.WriteAllLines(_file, lines);
            }
            else
                throw new Exception("Category not found.");
        }

        private int ParseIdFromLine(string line)
        {
            var idStr = line.Substring(0, ID_LENGTH);
            if (int.TryParse(idStr, out int id)) return id;
            return 0;
        }

        private Category ParseLine(string line)
        {
            try
            {
                int index = 0;
                string idS = line.Substring(index, ID_LENGTH); index += ID_LENGTH;
                string name = line.Substring(index, NAME_LENGTH); index += NAME_LENGTH;
                string desc = line.Substring(index, DESCRIPTION_LENGTH);

                return new Category
                {
                    Id = int.TryParse(idS, out var i) ? i : 0,
                    Name = name.Trim(),
                    Description = desc.Trim()
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse category line. " + ex.Message);
            }
        }

        private string BuildLine(Category c)
        {
            string id = c.Id.ToString().PadLeft(ID_LENGTH, '0');
            string name = (c.Name ?? string.Empty).PadRight(NAME_LENGTH).Substring(0, NAME_LENGTH);
            string desc = (c.Description ?? string.Empty).PadRight(DESCRIPTION_LENGTH).Substring(0, DESCRIPTION_LENGTH);
            return string.Concat(id, name, desc);
        }
    }
}