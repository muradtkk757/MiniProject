using System;
using System.IO;

namespace DataAccessLayer.Data
{
    public static class Data
    {
       
        public static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

      
        public static readonly string BooksFile = Path.Combine(DataFolder, "books.txt");
        public static readonly string CategoriesFile = Path.Combine(DataFolder, "categories.txt");
        public static readonly string MembersFile = Path.Combine(DataFolder, "members.txt");

   
        public static void EnsureDataFiles()
        {
            try
            {
               
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }

               
                if (!File.Exists(BooksFile)) File.WriteAllText(BooksFile, string.Empty);
                if (!File.Exists(CategoriesFile)) File.WriteAllText(CategoriesFile, string.Empty);
                if (!File.Exists(MembersFile)) File.WriteAllText(MembersFile, string.Empty);
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Data error: {ex.Message}");
            }
        }
    }
}