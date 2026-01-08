using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Data;
using DataAccessLayer.Repostories;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ConsoleUI
{
    class Program
    {
        static BookRepository bookRepo = new BookRepository();
        static CategoryRepository categoryRepo = new CategoryRepository();
        static MemberRepository memberRepo = new MemberRepository();

        static IBookService bookService = new BookService(bookRepo, categoryRepo);
        static ICategoryService categoryService = new CategoryService(categoryRepo);
        static IMemberService memberService = new MemberService(memberRepo);

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            UI.EnableVirtualTerminalProcessing();

            Data.EnsureDataFiles();

            LocalSettings.Load();

            Localization.SelectLanguageInteractive();
            UI.WriteSuccess("✅ " + UI.T("LanguageSaved"));

            UI.SpinnerAnsi(UI.T("Starting") + " 🚀", 700, 50, 200, 100);

            UI.ClearAndRenderHeader();

            while (true)
            {
                try
                {
                    UI.WriteLine("");
                    UI.WriteLine(UI.T("MainMenuPrefix"));
                    UI.WriteLine($"1. 📚 {UI.T("ManageBooks")}");
                    UI.WriteLine($"2. 🗂️ {UI.T("ManageCategories")}");
                    UI.WriteLine($"3. 👥 {UI.T("ManageMembers")}");
                    UI.WriteLine($"0. 🚪 {UI.T("Exit")}");
                    UI.Write(UI.T("Choose") + " ");
                    var k = Console.ReadLine();
                    if (k == "0") break;
                    switch (k)
                    {
                        case "1": BookMenu(); UI.ClearAndRenderHeader(); break;
                        case "2": CategoryMenu(); UI.ClearAndRenderHeader(); break;
                        case "3": MemberMenu(); UI.ClearAndRenderHeader(); break;
                        default: UI.WriteError("❌ " + UI.T("InvalidChoice")); break;
                    }
                }
                catch (Exception ex)
                {
                    UI.WriteError($"❗ {UI.T("Error")}: {ex.Message}");
                }
            }

            UI.WriteInfo("👋 " + UI.T("Goodbye"));
            Thread.Sleep(300);
        }

        static string GenerateUniqueIsbn()
        {
            Random random = new Random();
            string newIsbn;
            bool exists;
            do
            {
                int part1 = random.Next(100, 1000);
                int part2 = random.Next(100, 1000);
                int part3 = random.Next(100, 1000);

                newIsbn = $"9-{part1}-{part2}-{part3}";

                exists = bookService.GetAll().Any(b => b.ISBN == newIsbn);
            } while (exists);
            return newIsbn;
        }

        #region BookMenu
        static void BookMenu()
        {
            UI.ClearAndRenderHeader();
            UI.WriteLine("");
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));

            int areaStart = Console.CursorTop;
            while (true)
            {
                Console.SetCursorPosition(0, areaStart);
                UI.ClearFromLine(areaStart);
                UI.Write(UI.T("Choice") + " ");
                var c = Console.ReadLine();
                if (c == "0") return;
                switch (c)
                {
                    case "1": CreateBookFlow(areaStart); break;
                    case "2": ListBooksFlow(areaStart); break;
                    case "3": GetBookByIdFlow(areaStart); break;
                    case "4": UpdateBookFlow(areaStart); break;
                    case "5": DeleteBookFlow(areaStart); break;
                    case "6": SearchBooksFlow(areaStart); break;
                    default: UI.DisplayTransientMessage("❌ " + UI.T("InvalidChoice"), 900, areaStart); break;
                }
            }
        }

        static void CreateBookFlow(int areaStart)
        {
            int start = Console.CursorTop;
            try
            {
                UI.Write("🖊️ " + UI.T("Title") + ": ");
                var title = Console.ReadLine();

                UI.Write("✍️ " + UI.T("Author") + ": ");
                var author = Console.ReadLine();

                string isbn = GenerateUniqueIsbn();
                UI.WriteColoredLine("🔢 " + UI.T("ISBN") + " (Auto): " + isbn, ConsoleColor.Cyan);

                int currentYear = DateTime.Now.Year;
                UI.Write($"📅 {UI.T("PublishedYear")} ({UI.T("Default")} {currentYear}): ");
                var yearS = Console.ReadLine();
                int year = string.IsNullOrWhiteSpace(yearS) ? currentYear : (int.TryParse(yearS, out var y) ? y : currentYear);

                UI.WriteLine("");
                UI.WriteColoredLine("--- " + UI.T("ManageCategories") + " ---", ConsoleColor.Magenta);
                var categories = categoryService.GetAll();
                foreach (var cat in categories)
                {
                    UI.WriteLine($"{cat.Id}: {cat.Name}");
                }
                UI.WriteLine(new string('-', 30));

                UI.WriteColoredLine("1: " + UI.T("SelectExistingCategory"), ConsoleColor.Cyan);
                UI.WriteColoredLine("2: " + UI.T("CreateNewCategory"), ConsoleColor.Green);
                UI.Write(UI.T("Choice") + " ");
                var catChoice = Console.ReadLine();

                int selectedCategoryId = 0;

                if (catChoice == "2")
                {
                    UI.Write("🆕 " + UI.T("NewCategoryName") + ": ");
                    var newCatName = Console.ReadLine();
                    UI.Write("📝 " + UI.T("Description") + ": ");
                    var newCatDesc = Console.ReadLine();

                    var newCategory = new Category { Name = newCatName, Description = newCatDesc };
                    categoryService.Create(newCategory);
                    selectedCategoryId = newCategory.Id;
                    UI.WriteSuccess("✅ " + UI.T("NewCategoryCreatedSelected"));
                }
                else
                {
                    UI.Write("🗂️ " + UI.T("EnterCategoryId") + ": ");
                    var catIdInput = Console.ReadLine();
                    int.TryParse(catIdInput, out selectedCategoryId);
                }

                var book = new Book
                {
                    Title = title,
                    Author = author,
                    ISBN = isbn,
                    PublishedYear = year,
                    CategoryId = selectedCategoryId,
                    IsAvailable = true
                };

                bookService.Create(book);
                UI.DisplayTransientMessage("✅ " + UI.T("BookCreated"), 1500, areaStart);
            }
            catch (Exception ex)
            {
                UI.DisplayTransientMessage("❗ " + UI.T("Error") + ": " + ex.Message, 1400, start);
                UI.ClearFromLine(areaStart);
            }
        }
        static void ListBooksFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.SpinnerAnsi("🔎 " + UI.T("Loading"), 600, 30, 144, 255);
            var list = bookService.GetAll();
            var categories = categoryService.GetAll();
            var members = memberService.GetAll();

            if (!list.Any())
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NoBooks"), 900, start);
            }
            else
            {
                string format = "{0,-4} {1,-22} {2,-18} {3,-13} {4,-6} {5,-12} {6,-3} {7,-15}";
                // BURADA DÜZƏLİŞ EDİLDİ: UI.T("ColCurrentMember")
                UI.WriteColoredLine(string.Format(format, "ID", UI.T("ColTitle"), UI.T("ColAuthor"), "ISBN", UI.T("ColYear"), UI.T("ColCat"), "Sts", UI.T("ColCurrentMember")), ConsoleColor.Yellow);
                UI.WriteLine(new string('-', 100));

                int idx = 0;
                foreach (var b in list)
                {
                    var catName = categories.FirstOrDefault(c => c.Id == b.CategoryId)?.Name ?? "-";
                    string title = b.Title.Length > 20 ? b.Title.Substring(0, 19) + ".." : b.Title;
                    string author = b.Author.Length > 16 ? b.Author.Substring(0, 15) + ".." : b.Author;
                    string cat = catName.Length > 10 ? catName.Substring(0, 9) + ".." : catName;

                    string memberName = "";
                    if (b.CurrentMemberId > 0)
                    {
                        var m = members.FirstOrDefault(x => x.Id == b.CurrentMemberId);
                        if (m != null) memberName = m.FullName.Length > 14 ? m.FullName.Substring(0, 14) + "." : m.FullName;
                    }

                    string status = b.IsAvailable ? "+" : "-";
                    var color = (idx % 2 == 0) ? ConsoleColor.Gray : ConsoleColor.DarkGray;

                    UI.WriteColoredLine(string.Format(format, b.Id, title, author, b.ISBN, b.PublishedYear, cat, status, memberName), color);
                    idx++;
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
        }

        static void GetBookByIdFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else
            {
                var b = bookService.Get(id);
                if (b == null) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start); UI.ClearFromLine(areaStart); }
                else
                {
                    var catName = categoryService.Get(b.CategoryId)?.Name ?? "Yoxdur";
                    UI.WriteColoredLine($"{b.Id}: {b.Title} | {b.Author} | ISBN:{b.ISBN} | Year:{b.PublishedYear} | Cat: {catName} | Available:{b.IsAvailable}", ConsoleColor.White);
                    UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
                }
            }
        }

        static void UpdateBookFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else
            {
                var existing = bookService.Get(id);
                if (existing == null) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start); UI.ClearFromLine(areaStart); }
                else
                {
                    UI.Write($"🖊️ {UI.T("Title")} ({existing.Title}): "); var title = Console.ReadLine();
                    UI.Write($"✍️ {UI.T("Author")} ({existing.Author}): "); var author = Console.ReadLine();
                    UI.Write($"🔢 {UI.T("ISBN")} ({existing.ISBN}): "); var isbn = Console.ReadLine();
                    UI.Write($"📅 {UI.T("PublishedYear")} ({existing.PublishedYear}): "); var yearS = Console.ReadLine();
                    UI.Write($"🗂️ {UI.T("CategoryId")} ({existing.CategoryId}): "); var catS = Console.ReadLine();
                    UI.Write($"✅ {UI.T("IsAvailable")} ({existing.IsAvailable}): "); var availS = Console.ReadLine();

                    existing.Title = string.IsNullOrWhiteSpace(title) ? existing.Title : title;
                    existing.Author = string.IsNullOrWhiteSpace(author) ? existing.Author : author;
                    existing.ISBN = string.IsNullOrWhiteSpace(isbn) ? existing.ISBN : isbn;
                    existing.PublishedYear = int.TryParse(yearS, out var yy) ? yy : existing.PublishedYear;
                    existing.CategoryId = int.TryParse(catS, out var cc) ? cc : existing.CategoryId;
                    if (!string.IsNullOrWhiteSpace(availS) && (availS == "1" || availS.ToLower() == "true")) existing.IsAvailable = true;
                    else if (!string.IsNullOrWhiteSpace(availS) && (availS == "0" || availS.ToLower() == "false")) existing.IsAvailable = false;

                    bookService.Update(existing);
                    UI.DisplayTransientMessage("✅ Book updated successfully!", 1500, areaStart);
                }
            }
        }

        static void DeleteBookFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else { bookService.Delete(id); UI.DisplayTransientMessage("🗑️ Book deleted successfully!", 1500, areaStart); }
        }

        static void SearchBooksFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔎 " + UI.T("Keyword") + " (Ad, Avtor və ya Kateqoriya): ");
            var kw = Console.ReadLine()?.ToLower();
            if (string.IsNullOrWhiteSpace(kw)) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start); UI.ClearFromLine(areaStart); return; }

            var allBooks = bookService.GetAll();
            var allCategories = categoryService.GetAll();
            var res = allBooks.Where(b => b.Title.ToLower().Contains(kw) || b.Author.ToLower().Contains(kw) || (allCategories.FirstOrDefault(c => c.Id == b.CategoryId)?.Name.ToLower().Contains(kw) ?? false)).ToList();

            if (!res.Any()) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start); UI.ClearFromLine(areaStart); }
            else
            {
                string format = "{0,-4} {1,-25} {2,-20} {3,-15} {4,-15}";
                UI.WriteLine("");
                UI.WriteColoredLine(string.Format(format, "ID", UI.T("ColTitle"), UI.T("ColAuthor"), "ISBN", UI.T("ColCat")), ConsoleColor.Yellow);
                UI.WriteLine(new string('-', 85));
                foreach (var b in res)
                {
                    var catName = allCategories.FirstOrDefault(c => c.Id == b.CategoryId)?.Name ?? "Yoxdur";
                    string title = b.Title.Length > 22 ? b.Title.Substring(0, 22) + ".." : b.Title;
                    string author = b.Author.Length > 17 ? b.Author.Substring(0, 17) + ".." : b.Author;
                    string cat = catName.Length > 12 ? catName.Substring(0, 12) + ".." : catName;
                    UI.WriteColoredLine(string.Format(format, b.Id, title, author, b.ISBN, cat), ConsoleColor.DarkBlue);
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
        }
        #endregion

        #region CategoryMenu
        static void CategoryMenu()
        {
            UI.ClearAndRenderHeader();
            UI.WriteLine("");
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));

            int areaStart = Console.CursorTop;
            while (true)
            {
                Console.SetCursorPosition(0, areaStart);
                UI.ClearFromLine(areaStart);
                UI.Write(UI.T("Choice") + " ");
                var c = Console.ReadLine();
                if (c == "0") return;
                switch (c)
                {
                    case "1": CreateCategoryFlow(areaStart); break;
                    case "2": ListCategoriesFlow(areaStart); break;
                    case "3": GetCategoryByIdFlow(areaStart); break;
                    case "4": UpdateCategoryFlow(areaStart); break;
                    case "5": DeleteCategoryFlow(areaStart); break;
                    case "6": SearchCategoriesFlow(areaStart); break;
                    default: UI.DisplayTransientMessage("❌ " + UI.T("InvalidChoice"), 900, areaStart); break;
                }
            }
        }

        static void CreateCategoryFlow(int areaStart)
        {
            int start = Console.CursorTop;
            try
            {
                UI.Write("🖊️ " + UI.T("Name") + ": "); var name = Console.ReadLine();
                UI.Write("✍️ " + UI.T("Description") + ": "); var desc = Console.ReadLine();
                var c = new Category { Name = name, Description = desc };
                categoryService.Create(c);
                UI.DisplayTransientMessage("✅ Category added successfully!", 1500, areaStart);
            }
            catch (Exception ex)
            {
                UI.DisplayTransientMessage("❗ " + UI.T("Error") + ": " + ex.Message, 1400, start);
                UI.ClearFromLine(areaStart);
            }
        }

        static void ListCategoriesFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.SpinnerAnsi("🔎 " + UI.T("Loading"), 500, 199, 21, 133);
            var categories = categoryService.GetAll();
            var allBooks = bookService.GetAll();

            if (!categories.Any())
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NoCategories"), 900, start);
                UI.ClearFromLine(areaStart);
            }
            else
            {
                string format = "{0,-5} {1,-20} {2,-30}";
                UI.WriteColoredLine(string.Format(format, UI.T("ColCatId"), UI.T("ColCatName"), UI.T("ColCatDesc")), ConsoleColor.Magenta);
                UI.WriteLine(new string('-', 60));

                foreach (var c in categories)
                {
                    UI.WriteColoredLine(string.Format(format, c.Id, c.Name, c.Description), ConsoleColor.White);
                    var catBooks = allBooks.Where(b => b.CategoryId == c.Id).ToList();
                    if (catBooks.Any())
                    {
                        foreach (var b in catBooks)
                        {
                            UI.WriteColoredLine($"      └── 📖 {b.Title} ({b.Author})", ConsoleColor.DarkGray);
                        }
                    }
                    else
                    {
                        UI.WriteColoredLine("      └── (Kitab yoxdur)", ConsoleColor.DarkGray);
                    }
                    UI.WriteLine("");
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
        }

        static void GetCategoryByIdFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else
            {
                var c = categoryService.Get(id);
                if (c == null) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start); UI.ClearFromLine(areaStart); }
                else
                {
                    UI.WriteColoredLine($"{c.Id}: {c.Name} | {c.Description}", ConsoleColor.Magenta);
                    UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
                }
            }
        }

        static void UpdateCategoryFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else
            {
                var existing = categoryService.Get(id);
                if (existing == null) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start); UI.ClearFromLine(areaStart); }
                else
                {
                    UI.Write($"🖊️ {UI.T("Name")} ({existing.Name}): "); var name = Console.ReadLine();
                    UI.Write($"✍️ {UI.T("Description")} ({existing.Description}): "); var desc = Console.ReadLine();
                    existing.Name = string.IsNullOrWhiteSpace(name) ? existing.Name : name;
                    existing.Description = string.IsNullOrWhiteSpace(desc) ? existing.Description : desc;
                    categoryService.Update(existing);
                    UI.DisplayTransientMessage("✅ Category updated successfully!", 1500, areaStart);
                }
            }
        }

        static void DeleteCategoryFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else { categoryService.Delete(id); UI.DisplayTransientMessage("🗑️ Category deleted successfully!", 1500, areaStart); }
        }

        static void SearchCategoriesFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔎 " + UI.T("Keyword") + " (" + UI.T("Name") + "): "); var kw = Console.ReadLine();
            var res = categoryService.Search(kw);
            var allBooks = bookService.GetAll();

            if (!res.Any())
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start);
                UI.ClearFromLine(areaStart);
            }
            else
            {
                string format = "{0,-5} {1,-20}";
                UI.WriteColoredLine(string.Format(format, UI.T("ColCatId"), UI.T("ColCatName")), ConsoleColor.Magenta);
                UI.WriteLine(new string('-', 30));

                foreach (var c in res)
                {
                    UI.WriteColoredLine(string.Format(format, c.Id, c.Name), ConsoleColor.White);
                    var catBooks = allBooks.Where(b => b.CategoryId == c.Id).ToList();
                    if (catBooks.Any())
                    {
                        foreach (var b in catBooks)
                        {
                            UI.WriteColoredLine($"      └── 📖 {b.Title}", ConsoleColor.DarkGray);
                        }
                    }
                    else
                    {
                        UI.WriteColoredLine("      └── (Kitab yoxdur)", ConsoleColor.DarkGray);
                    }
                    UI.WriteLine("");
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
        }
        #endregion

        #region MemberMenu
        static void MemberMenu()
        {
            UI.ClearAndRenderHeader();
            UI.WriteLine("");
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));

            int areaStart = Console.CursorTop;
            while (true)
            {
                Console.SetCursorPosition(0, areaStart);
                UI.ClearFromLine(areaStart);
                UI.Write(UI.T("Choice") + " ");
                var c = Console.ReadLine();
                if (c == "0") return;
                switch (c)
                {
                    case "1": CreateMemberFlow(areaStart); break;
                    case "2": ListMembersFlow(areaStart); break;
                    case "3": GetMemberByIdFlow(areaStart); break;
                    case "4": UpdateMemberFlow(areaStart); break;
                    case "5": DeleteMemberFlow(areaStart); break;
                    case "6": SearchMembersFlow(areaStart); break;
                    default: UI.DisplayTransientMessage("❌ " + UI.T("InvalidChoice"), 900, areaStart); break;
                }
            }
        }

        static void CreateMemberFlow(int areaStart)
        {
            int start = Console.CursorTop;
            try
            {
                UI.Write("🧾 " + UI.T("FullName") + ": "); var name = Console.ReadLine();
                UI.Write("📧 " + UI.T("Email") + ": "); var email = Console.ReadLine();
                UI.Write("📱 " + UI.T("PhoneNumber") + ": "); var phone = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    UI.DisplayTransientMessage("❌ " + UI.T("NameRequired"), 1400, start);
                    UI.ClearFromLine(areaStart);
                    return;
                }

                var existingMembers = memberService.GetAll();
                bool duplicateExists = existingMembers.Any(m =>
                    m.Email.ToLower() == email.ToLower().Trim() ||
                    m.FullName.ToLower() == name.ToLower().Trim());

                if (duplicateExists)
                {
                    UI.DisplayTransientMessage("❌ " + UI.T("DuplicateMemberError"), 2000, start);
                    UI.ClearFromLine(areaStart);
                    return;
                }

                var mem = new Member { FullName = name, Email = email, PhoneNumber = phone, MembershipDate = DateTime.Now, IsActive = true };
                memberService.Create(mem);
                UI.DisplayTransientMessage("✅ " + UI.T("MemberCreated"), 1500, areaStart);
            }
            catch (Exception ex)
            {
                UI.DisplayTransientMessage("❗ " + UI.T("Error") + ": " + ex.Message, 1400, start);
                UI.ClearFromLine(areaStart);
            }
        }

        static void ListMembersFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.SpinnerAnsi("🔎 " + UI.T("Loading"), 500, 0, 200, 200);
            var list = memberService.GetAll();
            var books = bookService.GetAll();

            if (!list.Any())
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NoMembers"), 900, start);
                UI.ClearFromLine(areaStart);
            }
            else
            {
                string format = "{0,-5} {1,-20} {2,-25} {3,-15} {4,-5} {5,-20}";
                // BURADA DÜZƏLİŞ EDİLDİ: UI.T("ColCurrentBook")
                UI.WriteColoredLine(string.Format(format, "ID", UI.T("ColMemName"), UI.T("ColMemEmail"), UI.T("ColMemPhone"), "Sts", UI.T("ColCurrentBook")), ConsoleColor.Cyan);
                UI.WriteLine(new string('-', 100));

                int idx = 0;
                foreach (var m in list)
                {

                    string bookTitle = "";
                    if (m.CurrentBookId > 0)
                    {
                        var b = books.FirstOrDefault(x => x.Id == m.CurrentBookId);
                        if (b != null) bookTitle = b.Title.Length > 18 ? b.Title.Substring(0, 18) + "." : b.Title;
                    }

                    var color = (idx % 2 == 0) ? ConsoleColor.Gray : ConsoleColor.DarkGray;
                    UI.WriteColoredLine(string.Format(format, m.Id, m.FullName, m.Email, m.PhoneNumber, m.IsActive ? "+" : "-", bookTitle), color);
                    idx++;
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
        }

        static void GetMemberByIdFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else
            {
                var m = memberService.Get(id);
                if (m == null) { UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start); UI.ClearFromLine(areaStart); }
                else
                {
                    UI.WriteColoredLine($"{m.Id}: {m.FullName} | {m.Email} | {m.PhoneNumber} | {m.MembershipDate:yyyy-MM-dd} | Active:{m.IsActive}", ConsoleColor.Cyan);
                    UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
                }
            }
        }

        static void UpdateMemberFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": ");
            var idS = Console.ReadLine();

            if (!int.TryParse(idS, out int id))
            {
                UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
                UI.ClearFromLine(areaStart);
                return;
            }

            var existingMember = memberService.Get(id);
            if (existingMember == null)
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                UI.ClearFromLine(areaStart);
                return;
            }

            UI.Write($"🧾 {UI.T("FullName")} ({existingMember.FullName}): ");
            var name = Console.ReadLine();
            UI.Write($"📧 {UI.T("Email")} ({existingMember.Email}): ");
            var email = Console.ReadLine();
            UI.Write($"📱 {UI.T("PhoneNumber")} ({existingMember.PhoneNumber}): ");
            var phone = Console.ReadLine();

            string currentBookTitle = UI.T("NoResults");
            if (existingMember.CurrentBookId > 0)
            {
                var b = bookService.Get(existingMember.CurrentBookId);
                if (b != null) currentBookTitle = b.Title;
            }
            UI.WriteColoredLine($"📖 {UI.T("CurrentlyReading")}: {currentBookTitle}", ConsoleColor.Yellow);

            UI.Write(UI.T("ChangeBookPrompt") + " ");
            var changeBook = Console.ReadLine();

            if (changeBook == "1")
            {
                UI.WriteLine(UI.T("BooksHeader"));
                var availBooks = bookService.GetAll();
                foreach (var b in availBooks)
                {
                    string statusKey = b.IsAvailable ? "StatusEmpty" : (b.CurrentMemberId == existingMember.Id ? "StatusCurrent" : "StatusOccupied");
                    Console.ForegroundColor = b.IsAvailable ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"{b.Id}: {b.Title} {UI.T(statusKey)}");
                    Console.ResetColor();
                }

                UI.Write(UI.T("EnterNewBookId") + ": ");
                var newBookIdS = Console.ReadLine();

                if (int.TryParse(newBookIdS, out int newBookId) && newBookId > 0)
                {
                    var newBook = bookService.Get(newBookId);
                    if (newBook != null)
                    {
                        if (existingMember.CurrentBookId > 0)
                        {
                            var oldBook = bookService.Get(existingMember.CurrentBookId);
                            if (oldBook != null)
                            {
                                oldBook.IsAvailable = true;
                                oldBook.CurrentMemberId = 0;
                                bookService.Update(oldBook);
                            }
                        }

                        if (newBook.IsAvailable || newBook.CurrentMemberId == existingMember.Id)
                        {
                            newBook.IsAvailable = false;
                            newBook.CurrentMemberId = existingMember.Id;
                            bookService.Update(newBook);

                            existingMember.CurrentBookId = newBook.Id;
                            existingMember.IsActive = true;
                        }
                        else
                        {
                            UI.WriteError("❌ " + UI.T("BookOccupiedError"));
                        }
                    }
                    else
                    {
                        UI.WriteError("❌ " + UI.T("BookNotFoundError"));
                    }
                }
                else
                {
                    if (existingMember.CurrentBookId > 0)
                    {
                        var oldBook = bookService.Get(existingMember.CurrentBookId);
                        if (oldBook != null)
                        {
                            oldBook.IsAvailable = true;
                            oldBook.CurrentMemberId = 0;
                            bookService.Update(oldBook);
                        }
                    }
                    existingMember.CurrentBookId = 0;
                    existingMember.IsActive = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(name)) existingMember.FullName = name;
            if (!string.IsNullOrWhiteSpace(email)) existingMember.Email = email;
            if (!string.IsNullOrWhiteSpace(phone)) existingMember.PhoneNumber = phone;

            memberService.Update(existingMember);
            UI.DisplayTransientMessage("✅ " + UI.T("MemberUpdated"), 1500, areaStart);
        }
        static void DeleteMemberFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) { UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start); UI.ClearFromLine(areaStart); }
            else { memberService.Delete(id); UI.DisplayTransientMessage("🗑️ " + UI.T("Deleted"), 1500, areaStart); }
        }

        static void SearchMembersFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔎 " + UI.T("Keyword") + " (" + UI.T("NameOrEmail") + "): "); var kw = Console.ReadLine();
            var res = memberService.Search(kw);
            if (!res.Any())
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start);
                UI.ClearFromLine(areaStart);
            }
            else
            {
                string format = "{0,-5} {1,-25} {2,-30} {3,-15} {4,-10}";
                UI.WriteColoredLine(string.Format(format, UI.T("ColMemId"), UI.T("ColMemName"), UI.T("ColMemEmail"), UI.T("ColMemPhone"), UI.T("ColMemStatus")), ConsoleColor.Cyan);
                UI.WriteLine(new string('-', 100));
                foreach (var m in res)
                {
                    UI.WriteColoredLine(string.Format(format, m.Id, m.FullName, m.Email, m.PhoneNumber, m.IsActive ? "+" : "-"), ConsoleColor.DarkCyan);
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
        }
        #endregion
    }

    public class LocalSettings
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library_settings.json");
        public static LocalSettings Instance { get; private set; } = new LocalSettings();

        public string Language { get; set; } = "az";

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    Instance = JsonSerializer.Deserialize<LocalSettings>(json) ?? new LocalSettings();
                }
                else
                {
                    Instance = new LocalSettings();
                    Instance.Save();
                }
                UI.SetLanguage(Instance.Language);
                Localization.SetLanguage(Instance.Language);
            }
            catch
            {
                Instance = new LocalSettings();
                Instance.Save();
                UI.SetLanguage(Instance.Language);
                Localization.SetLanguage(Instance.Language);
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch
            {
            }
            UI.SetLanguage(this.Language);
            Localization.SetLanguage(this.Language);
        }
    }

    public static class UI
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);
        private const int STD_OUTPUT_HANDLE = -11;
        private const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        public static void EnableVirtualTerminalProcessing()
        {
            try
            {
                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(handle, out int mode))
                {
                    mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    SetConsoleMode(handle, mode);
                }
            }
            catch
            {
            }
        }

        public static void SetLanguage(string language)
        {
            Localization.SetLanguage(language);
        }

        public static string T(string key)
        {
            return Localization.T(key);
        }

        public static void Clear()
        {
            Console.Clear();
        }

        public static void ClearAndRenderHeader()
        {
            Console.Clear();
            WriteHeader(T("LibraryTitle"));
        }

        public static void ClearFromLine(int top)
        {
            try
            {
                int width = Console.WindowWidth;
                int height = Console.WindowHeight;
                for (int row = top; row < height; row++)
                {
                    Console.SetCursorPosition(0, row);
                    Console.Write(new string(' ', width));
                }
                Console.SetCursorPosition(0, top);
            }
            catch
            {
                Console.Clear();
                WriteHeader(T("LibraryTitle"));
            }
        }

        public static void ClearLastLines(int count)
        {
            if (count <= 0) return;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (Console.CursorTop > 0)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop - 1 >= 0 ? Console.CursorTop - 1 : 0);
                    }
                }
            }
            catch
            {
                Console.Clear();
                WriteHeader(T("LibraryTitle"));
            }
        }

        public static void DisplayTransientMessage(string text, int durationMs = 900, int areaStart = -1)
        {
            Console.WriteLine(text);
            Thread.Sleep(durationMs);
            if (areaStart >= 0)
                ClearFromLine(areaStart);
            else
                ClearLastLines(1);
        }

        public static void WaitForEnterAndClearFrom(int areaStart, string prompt = null)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                Console.WriteLine(prompt);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(UI.T("PressEnterToContinue"));
            }

            Console.ReadLine();
            ClearFromLine(areaStart);
        }
        public static void WriteHeader(string text)
        {
            try
            {
                int width = Console.WindowWidth;
                string content = " " + text + " ";
                if (width < 10 || content.Length >= width - 2)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(new string('=', Math.Max(0, width - 1)));
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(text);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(new string('=', Math.Max(0, width - 1)));
                    Console.ResetColor();
                    return;
                }

                int availableForEquals = width - content.Length;
                int leftEquals = availableForEquals / 2;
                int rightEquals = availableForEquals - leftEquals;

                if (leftEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(new string('=', leftEquals));
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string('=', content.Length));
                if (rightEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(new string('=', rightEquals));
                }
                Console.WriteLine();
                Console.ResetColor();

                if (leftEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(new string('=', leftEquals));
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(content);

                if (rightEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(new string('=', rightEquals));
                }
                Console.WriteLine();
                Console.ResetColor();

                if (leftEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(new string('=', leftEquals));
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(new string('=', content.Length));
                if (rightEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(new string('=', rightEquals));
                }
                Console.WriteLine();
                Console.ResetColor();
            }
            catch
            {
                Console.WriteLine("=================================================");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(text);
                Console.ResetColor();
                Console.WriteLine("=================================================");
            }
        }
        public static void WriteLine(string text)
        {
            Console.ResetColor();
            Console.WriteLine(text);
        }

        public static void Write(string text)
        {
            Console.ResetColor();
            Console.Write(text);
        }

        public static void WriteSuccess(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteInfo(string text)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteColoredLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteColored(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteRgbLine(string text, int r, int g, int b)
        {
            string seq = $"\u001b[38;2;{r};{g};{b}m{text}\u001b[0m";
            Console.WriteLine(seq);
        }

        public static void WriteRgb(string text, int r, int g, int b)
        {
            string seq = $"\u001b[38;2;{r};{g};{b}m{text}\u001b[0m";
            Console.Write(seq);
        }

        public static void Spinner(string message, int durationMs = 800)
        {
            var spinner = new[] { '|', '/', '-', '\\' };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int idx = 0;
            Console.Write(message + " ");
            while (sw.ElapsedMilliseconds < durationMs)
            {
                Console.Write(spinner[idx++ % spinner.Length]);
                Thread.Sleep(80);
                Console.Write("\b");
            }
            Console.WriteLine();
        }

        public static void SpinnerAnsi(string message, int durationMs = 800, int r = 255, int g = 255, int b = 255)
        {
            var spinner = new[] { '|', '/', '-', '\\' };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int idx = 0;
            WriteRgb(message + " ", r, g, b);
            while (sw.ElapsedMilliseconds < durationMs)
            {
                WriteRgb(spinner[idx++ % spinner.Length].ToString(), r, g, b);
                Thread.Sleep(80);
                Console.Write("\b");
            }
            Console.WriteLine();
        }
    }
}