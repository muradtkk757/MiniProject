using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DataAccessLayer.Data
{
    public static class Data
    {
     
        public static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        
        public static readonly string BooksFile = Path.Combine(DataFolder, "books.txt");
        public static readonly string CategoriesFile = Path.Combine(DataFolder, "categories.txt");
        public static readonly string MembersFile = Path.Combine(DataFolder, "members.txt");

       
        private static readonly object _fileLock = new object();


        public static void EnsureDataFiles()
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }

                
                if (!File.Exists(BooksFile))
                    File.WriteAllText(BooksFile, string.Empty);

                if (!File.Exists(CategoriesFile))
                    File.WriteAllText(CategoriesFile, string.Empty);

                if (!File.Exists(MembersFile))
                    File.WriteAllText(MembersFile, string.Empty);
            }
            catch (Exception ex)
            {
               
                throw new Exception("Failed to ensure data files: " + ex.Message, ex);
            }
        }

    
        public static void AppendLine(string filePath, string line)
        {
            lock (_fileLock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }

      
        public static string[] ReadAllLines(string filePath)
        {
            if (!File.Exists(filePath)) return Array.Empty<string>();
            return File.ReadAllLines(filePath);
        }

       
        public static void WriteAllLines(string filePath, string[] lines)
        {
            lock (_fileLock)
            {
                File.WriteAllLines(filePath, lines);
            }
        }
    }
}