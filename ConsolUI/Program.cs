using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Repostories;
using DataAccessLayerModels;
using System;
using System.IO;
using System.Text;
using DataAccessLayer.Data;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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
            UI.EnableVirtualTerminalProcessing(); // enable ANSI truecolor where possible
            DataContext.EnsureDataFiles();

            // Load settings
            LocalSettings.Load();

            // Show language selection (delegated to Localization)
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

        #region BookMenu
        static void BookMenu()
        {
            // Print menu once and keep it visible; later we only clear action area
            UI.ClearAndRenderHeader();
            UI.WriteLine("");
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));

            // where action content (prompts, results) will start
            int actionAreaStart = Console.CursorTop;

            while (true)
            {
                UI.Write(UI.T("Choice") + " ");
                var c = Console.ReadLine();
                if (c == "0")
                {
                    // clear only action area and return (menu removed)
                    UI.ClearFromLine(actionAreaStart);
                    return;
                }

                switch (c)
                {
                    case "1":
                        CreateBookFlow(actionAreaStart);
                        break;
                    case "2":
                        ListBooksFlow(actionAreaStart);
                        break;
                    case "3":
                        GetBookByIdFlow(actionAreaStart);
                        break;
                    case "4":
                        UpdateBookFlow(actionAreaStart);
                        break;
                    case "5":
                        DeleteBookFlow(actionAreaStart);
                        break;
                    case "6":
                        SearchBooksFlow(actionAreaStart);
                        break;
                    default:
                        UI.DisplayTransientMessage("❌ " + UI.T("InvalidChoice"), 900, actionAreaStart);
                        break;
                }
            }
        }

        // Each flow uses areaStart to clear its own prompts/results after finish
        static void CreateBookFlow(int areaStart)
        {
            int start = Console.CursorTop;
            try
            {
                UI.Write("🖊️ " + UI.T("Title") + ": "); var title = Console.ReadLine();
                UI.Write("✍️ " + UI.T("Author") + ": "); var author = Console.ReadLine();
                UI.Write("🔢 " + UI.T("ISBN") + ": "); var isbn = Console.ReadLine();
                UI.Write("📅 " + UI.T("PublishedYear") + ": "); var yearS = Console.ReadLine();
                UI.Write("🗂️ " + UI.T("CategoryId") + ": "); var catS = Console.ReadLine();
                int year = int.TryParse(yearS, out var y) ? y : 0;
                int catId = int.TryParse(catS, out var cc) ? cc : 0;
                var book = new Book { Title = title, Author = author, ISBN = isbn, PublishedYear = year, CategoryId = catId, IsAvailable = true };
                bookService.Create(book);
                UI.DisplayTransientMessage("✅ " + UI.T("BookCreated"), 1000, start);
            }
            catch (Exception ex)
            {
                UI.DisplayTransientMessage("❗ " + UI.T("Error") + ": " + ex.Message, 1400, start);
            }
            finally
            {
                // clear action area (prompts + message)
                UI.ClearFromLine(areaStart);
                // reprint menu options area so user can choose again
                // (we'll re-print lines under header)
                UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
                UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
                UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
                UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
                UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
                UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
                UI.WriteLine("0: ◀️ " + UI.T("Back"));
            }
        }

        static void ListBooksFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.SpinnerAnsi("🔎 " + UI.T("Loading"), 600, 30, 144, 255);
            var list = bookService.GetAll();
            if (!list.Any())
            {
                UI.DisplayTransientMessage("ℹ️ " + UI.T("NoBooks"), 900, start);
            }
            else
            {
                int idx = 0;
                foreach (var b in list)
                {
                    var color = (idx % 2 == 0) ? ConsoleColor.Gray : ConsoleColor.DarkGray;
                    UI.WriteColoredLine($"{b.Id}: {b.Title} | {b.Author} | ISBN:{b.ISBN} | Year:{b.PublishedYear} | Cat:{b.CategoryId} | Available:{b.IsAvailable}", color);
                    idx++;
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
            UI.ClearFromLine(areaStart);
            // re-print menu
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void GetBookByIdFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id))
            {
                UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            }
            else
            {
                var b = bookService.Get(id);
                if (b == null) UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                else
                {
                    UI.WriteColoredLine($"{b.Id}: {b.Title} | {b.Author} | ISBN:{b.ISBN} | Year:{b.PublishedYear} | Cat:{b.CategoryId} | Available:{b.IsAvailable}", ConsoleColor.White);
                    UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
                }
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void UpdateBookFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id))
            {
                UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            }
            else
            {
                var existing = bookService.Get(id);
                if (existing == null)
                {
                    UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                }
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
                    UI.DisplayTransientMessage("✅ " + UI.T("Updated"), 1000, start);
                }
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void DeleteBookFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id))
            {
                UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            }
            else
            {
                bookService.Delete(id);
                UI.DisplayTransientMessage("🗑️ " + UI.T("Deleted"), 1000, start);
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void SearchBooksFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔎 " + UI.T("Keyword") + " (" + UI.T("TitleAuthorCatId") + "): "); var kw = Console.ReadLine();
            var res = bookService.Search(kw);
            if (!res.Any()) UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start);
            else
            {
                foreach (var b in res) UI.WriteColoredLine($"{b.Id}: {b.Title} | {b.Author} | Cat:{b.CategoryId}", ConsoleColor.DarkBlue);
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 30, 144, 255);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }
        #endregion

        #region CategoryMenu (same pattern)
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
                UI.Write(UI.T("Choice") + " ");
                var c = Console.ReadLine();
                if (c == "0") { UI.ClearFromLine(areaStart); return; }
                switch (c)
                {
                    case "1":
                        CreateCategoryFlow(areaStart);
                        break;
                    case "2":
                        ListCategoriesFlow(areaStart);
                        break;
                    case "3":
                        GetCategoryByIdFlow(areaStart);
                        break;
                    case "4":
                        UpdateCategoryFlow(areaStart);
                        break;
                    case "5":
                        DeleteCategoryFlow(areaStart);
                        break;
                    case "6":
                        SearchCategoriesFlow(areaStart);
                        break;
                    default:
                        UI.DisplayTransientMessage("❌ " + UI.T("InvalidChoice"), 900, areaStart);
                        break;
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
                UI.DisplayTransientMessage("✅ " + UI.T("CategoryCreated"), 1000, start);
            }
            catch (Exception ex) { UI.DisplayTransientMessage("❗ " + UI.T("Error") + ": " + ex.Message, 1400, start); }
            UI.ClearFromLine(areaStart);
            // reprint menu options
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void ListCategoriesFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.SpinnerAnsi("🔎 " + UI.T("Loading"), 500, 199, 21, 133);
            var list = categoryService.GetAll();
            if (!list.Any()) UI.DisplayTransientMessage("ℹ️ " + UI.T("NoCategories"), 900, start);
            else
            {
                int idx = 0;
                foreach (var c in list)
                {
                    var color = (idx % 2 == 0) ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta;
                    UI.WriteColoredLine($"{c.Id}: {c.Name} | {c.Description}", color);
                    idx++;
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void GetCategoryByIdFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            else
            {
                var c = categoryService.Get(id);
                if (c == null) UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                else
                {
                    UI.WriteColoredLine($"{c.Id}: {c.Name} | {c.Description}", ConsoleColor.Magenta);
                    UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
                }
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void UpdateCategoryFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            else
            {
                var existing = categoryService.Get(id);
                if (existing == null) UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                else
                {
                    UI.Write($"🖊️ {UI.T("Name")} ({existing.Name}): "); var name = Console.ReadLine();
                    UI.Write($"✍️ {UI.T("Description")} ({existing.Description}): "); var desc = Console.ReadLine();
                    existing.Name = string.IsNullOrWhiteSpace(name) ? existing.Name : name;
                    existing.Description = string.IsNullOrWhiteSpace(desc) ? existing.Description : desc;
                    categoryService.Update(existing);
                    UI.DisplayTransientMessage("✅ " + UI.T("Updated"), 1000, start);
                }
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void DeleteCategoryFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            else
            {
                categoryService.Delete(id);
                UI.DisplayTransientMessage("🗑️ " + UI.T("Deleted"), 1000, start);
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void SearchCategoriesFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔎 " + UI.T("Keyword") + " (" + UI.T("Name") + "): "); var kw = Console.ReadLine();
            var res = categoryService.Search(kw);
            if (!res.Any()) UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start);
            else
            {
                foreach (var c in res) UI.WriteColoredLine($"{c.Id}: {c.Name}", ConsoleColor.Magenta);
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 199, 21, 133);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }
        #endregion

        #region MemberMenu (same pattern)
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
                UI.Write(UI.T("Choice") + " ");
                var c = Console.ReadLine();
                if (c == "0") { UI.ClearFromLine(areaStart); return; }
                switch (c)
                {
                    case "1":
                        CreateMemberFlow(areaStart);
                        break;
                    case "2":
                        ListMembersFlow(areaStart);
                        break;
                    case "3":
                        GetMemberByIdFlow(areaStart);
                        break;
                    case "4":
                        UpdateMemberFlow(areaStart);
                        break;
                    case "5":
                        DeleteMemberFlow(areaStart);
                        break;
                    case "6":
                        SearchMembersFlow(areaStart);
                        break;
                    default:
                        UI.DisplayTransientMessage("❌ " + UI.T("InvalidChoice"), 900, areaStart);
                        break;
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
                var m = new Member { FullName = name, Email = email, PhoneNumber = phone, MembershipDate = DateTime.Now, IsActive = true };
                memberService.Create(m);
                UI.DisplayTransientMessage("✅ " + UI.T("MemberCreated"), 1000, start);
            }
            catch (Exception ex) { UI.DisplayTransientMessage("❗ " + UI.T("Error") + ": " + ex.Message, 1400, start); }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void ListMembersFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.SpinnerAnsi("🔎 " + UI.T("Loading"), 500, 0, 200, 200);
            var list = memberService.GetAll();
            if (!list.Any()) UI.DisplayTransientMessage("ℹ️ " + UI.T("NoMembers"), 900, start);
            else
            {
                int idx = 0;
                foreach (var m in list)
                {
                    var color = (idx % 2 == 0) ? ConsoleColor.Cyan : ConsoleColor.DarkCyan;
                    UI.WriteColoredLine($"{m.Id}: {m.FullName} | {m.Email} | {m.PhoneNumber} | {m.MembershipDate:yyyy-MM-dd} | Active:{m.IsActive}", color);
                    idx++;
                }
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void GetMemberByIdFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            else
            {
                var m = memberService.Get(id);
                if (m == null) UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                else
                {
                    UI.WriteColoredLine($"{m.Id}: {m.FullName} | {m.Email} | {m.PhoneNumber} | {m.MembershipDate:yyyy-MM-dd} | Active:{m.IsActive}", ConsoleColor.Cyan);
                    UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
                }
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void UpdateMemberFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            else
            {
                var existing = memberService.Get(id);
                if (existing == null) UI.DisplayTransientMessage("ℹ️ " + UI.T("NotFound"), 900, start);
                else
                {
                    UI.Write($"🧾 {UI.T("FullName")} ({existing.FullName}): "); var name = Console.ReadLine();
                    UI.Write($"📧 {UI.T("Email")} ({existing.Email}): "); var email = Console.ReadLine();
                    UI.Write($"📱 {UI.T("PhoneNumber")} ({existing.PhoneNumber}): "); var phone = Console.ReadLine();
                    UI.Write($"✅ {UI.T("IsActive")} ({existing.IsActive}): "); var activeS = Console.ReadLine();

                    existing.FullName = string.IsNullOrWhiteSpace(name) ? existing.FullName : name;
                    existing.Email = string.IsNullOrWhiteSpace(email) ? existing.Email : email;
                    existing.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? existing.PhoneNumber : phone;
                    if (!string.IsNullOrWhiteSpace(activeS) && (activeS == "1" || activeS.ToLower() == "true")) existing.IsActive = true;
                    else if (!string.IsNullOrWhiteSpace(activeS) && (activeS == "0" || activeS.ToLower() == "false")) existing.IsActive = false;

                    memberService.Update(existing);
                    UI.DisplayTransientMessage("✅ " + UI.T("Updated"), 1000, start);
                }
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void DeleteMemberFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔢 " + UI.T("Id") + ": "); var idS = Console.ReadLine();
            if (!int.TryParse(idS, out int id)) UI.DisplayTransientMessage("❌ " + UI.T("InvalidId"), 900, start);
            else
            {
                memberService.Delete(id);
                UI.DisplayTransientMessage("🗑️ " + UI.T("Deleted"), 1000, start);
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }

        static void SearchMembersFlow(int areaStart)
        {
            int start = Console.CursorTop;
            UI.Write("🔎 " + UI.T("Keyword") + " (" + UI.T("NameOrEmail") + "): "); var kw = Console.ReadLine();
            var res = memberService.Search(kw);
            if (!res.Any()) UI.DisplayTransientMessage("ℹ️ " + UI.T("NoResults"), 900, start);
            else
            {
                foreach (var m in res) UI.WriteColoredLine($"{m.Id}: {m.FullName} | {m.Email}", ConsoleColor.Cyan);
                UI.WaitForEnterAndClearFrom(areaStart, UI.T("PressEnterToContinue"));
            }
            UI.ClearFromLine(areaStart);
            UI.WriteColoredLine("1: 🟢 " + UI.T("Create"), ConsoleColor.Green);
            UI.WriteRgbLine("2: 🎯 " + UI.T("List"), 0, 200, 200);
            UI.WriteColoredLine("3: 🔎 " + UI.T("GetById"), ConsoleColor.White);
            UI.WriteColoredLine("4: ✏️ " + UI.T("Update"), ConsoleColor.Yellow);
            UI.WriteColoredLine("5: 🗑️ " + UI.T("Delete"), ConsoleColor.Red);
            UI.WriteColoredLine("6: 🔍 " + UI.T("Search"), ConsoleColor.Blue);
            UI.WriteLine("0: ◀️ " + UI.T("Back"));
        }
        #endregion
    }

    // LocalSettings (sizin əvvəlki implementation u qalır)
    public class LocalSettings
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library_settings.json");
        public static LocalSettings Instance { get; private set; } = new LocalSettings();

        public string Language { get; set; } = "az"; // default az

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
                // Apply saved language to UI/Localization
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
                // ignore
            }
            UI.SetLanguage(this.Language);
            Localization.SetLanguage(this.Language);
        }
    }

    // UI Helpers (delegates translations to Localization). Yeni: ClearFromLine, ClearLastLines, DisplayTransientMessage, WaitForEnterAndClearFrom.
    public static class UI
    {
        // Try to enable ANSI VT mode on Windows so truecolor sequences work
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
                // ignore - not critical
            }
        }

        // Delegation to Localization for translations
        public static void SetLanguage(string language)
        {
            Localization.SetLanguage(language);
        }

        public static string T(string key)
        {
            return Localization.T(key);
        }

        // Clear helpers
        public static void Clear()
        {
            Console.Clear();
        }

        public static void ClearAndRenderHeader()
        {
            Console.Clear();
            WriteHeader(T("LibraryTitle"));
        }

        // Clears console content starting from 'top' line to the bottom of visible window.
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
                // fallback
                Console.Clear();
                WriteHeader(T("LibraryTitle"));
            }
        }

        // Clear last N printed lines (from current cursor position upwards)
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

        // Display a transient message (e.g. "Added") for durationMs then remove it.
        // If areaStart provided, we'll leave content above areaStart intact and clear everything under it after timeout.
        public static void DisplayTransientMessage(string text, int durationMs = 900, int areaStart = -1)
        {
            Console.WriteLine(text);
            Thread.Sleep(durationMs);
            if (areaStart >= 0)
                ClearFromLine(areaStart);
            else
                ClearLastLines(1);
        }

        // Wait for Enter key and then clear content starting from areaStart.

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
                    // fallback: sadə üçsətrli başlıq
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(new string('=', Math.Max(0, width - 1)));
                    Console.ForegroundColor = ConsoleColor.Yellow; // başlıq mətni sarı
                    Console.WriteLine(text);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(new string('=', Math.Max(0, width - 1)));
                    Console.ResetColor();
                    return;
                }

                int availableForEquals = width - content.Length;
                int leftEquals = availableForEquals / 2;
                int rightEquals = availableForEquals - leftEquals;

                // Top border: sol (Cyan), orta (Red), sağ (Green)
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

                // Middle line: sol '=', mərkəzdə SARİ başlıq, sağ '='
                if (leftEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(new string('=', leftEquals));
                }

                Console.ForegroundColor = ConsoleColor.Yellow; // <-- başlıq mətni SARİ
                Console.Write(content);

                if (rightEquals > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(new string('=', rightEquals));
                }
                Console.WriteLine();
                Console.ResetColor();

                // Bottom border: eyni sxem
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
                // fallback sadə
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

        // Write truecolor (RGB) text using ANSI escape sequences.
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

        // Spinner using ConsoleColor
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

        // Spinner using ANSI RGB color for spinner/label
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