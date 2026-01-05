using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DataAccessLayer.Data
{
    public static class DataContext
    {
        // Base data folder relative to the running executable.
        // You can change this to a fixed path if needed.
        public static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        // Files used by repositories (names must match repositories' expectations)
        public static readonly string BooksFile = Path.Combine(DataFolder, "books.txt");
        public static readonly string CategoriesFile = Path.Combine(DataFolder, "categories.txt");
        public static readonly string MembersFile = Path.Combine(DataFolder, "members.txt");

        // A single lock for simple thread-safety on file writes/appends.
        private static readonly object _fileLock = new object();

        /// <summary>
        /// Ensure the Data folder and the three files exist.
        /// Call this once at application startup (e.g. Program.Main).
        /// </summary>
        public static void EnsureDataFiles()
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }

                // Create files if they do not exist. Leave empty if already present.
                if (!File.Exists(BooksFile))
                    File.WriteAllText(BooksFile, string.Empty);

                if (!File.Exists(CategoriesFile))
                    File.WriteAllText(CategoriesFile, string.Empty);

                if (!File.Exists(MembersFile))
                    File.WriteAllText(MembersFile, string.Empty);
            }
            catch (Exception ex)
            {
                // Bubble up or log as needed. Repositories expect files to exist.
                throw new Exception("Failed to ensure data files: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Thread-safe append a single line to a file.
        /// </summary>
        public static void AppendLine(string filePath, string line)
        {
            lock (_fileLock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }

        /// <summary>
        /// Read all lines from a file. If file missing, returns empty array.
        /// </summary>
        public static string[] ReadAllLines(string filePath)
        {
            if (!File.Exists(filePath)) return Array.Empty<string>();
            return File.ReadAllLines(filePath);
        }

        /// <summary>
        /// Replace file contents with given lines (overwrite).
        /// </summary>
        public static void WriteAllLines(string filePath, string[] lines)
        {
            lock (_fileLock)
            {
                File.WriteAllLines(filePath, lines);
            }
        }
    }
}