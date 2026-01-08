using DataAccessLayer.Data;
using DataAccessLayer.Repostories.Contracts;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataAccessLayer.Repostories
{
    public class MemberRepository : IRepository<Member>
    {
        private readonly string _file = DataAccessLayer.Data.Data.MembersFile;

        private const int ID_LENGTH = 5;
        private const int NAME_LENGTH = 25;
        private const int EMAIL_LENGTH = 30;
        private const int PHONE_LENGTH = 15;
        private const int ISACTIVE_LENGTH = 1;
        private const int BOOKID_LENGTH = 5;

        public MemberRepository()
        {
            DataAccessLayer.Data.Data.EnsureDataFiles();
        }

        public void Add(Member entity)
        {
            var allMembers = GetAll();
            int nextId = (allMembers.Any() ? allMembers.Max(m => m.Id) : 0) + 1;
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

        public List<Member> GetAll()
        {
            var result = new List<Member>();
            if (!File.Exists(_file)) return result;

            foreach (var line in File.ReadAllLines(_file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var m = ParseLine(line);
                if (m != null) result.Add(m);
            }
            return result;
        }

       
        public Member GetById(int id)
        {
            if (!File.Exists(_file)) return null;

            var line = File.ReadAllLines(_file)
                           .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && ParseIdFromLine(l) == id);

            return line == null ? null : ParseLine(line);
        }

        public void Update(Member entity)
        {
            var lines = File.ReadAllLines(_file).ToList();
            int idx = lines.FindIndex(l => ParseIdFromLine(l) == entity.Id);

            if (idx >= 0)
            {
                lines[idx] = BuildLine(entity);
                File.WriteAllLines(_file, lines);
            }
        }

        public List<Member> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<Member>();
            keyword = keyword.ToLower();
            return GetAll().Where(m => m.FullName.ToLower().Contains(keyword) || m.Email.ToLower().Contains(keyword)).ToList();
        }

        private int ParseIdFromLine(string line)
        {
            if (line.Length < ID_LENGTH) return 0;
            var idStr = line.Substring(0, ID_LENGTH);
            return int.TryParse(idStr, out int id) ? id : 0;
        }

        private Member ParseLine(string line)
        {
            try
            {
                int index = 0;
                string idS = line.Substring(index, ID_LENGTH); index += ID_LENGTH;
                string name = line.Substring(index, NAME_LENGTH).Trim(); index += NAME_LENGTH;
                string email = line.Substring(index, EMAIL_LENGTH).Trim(); index += EMAIL_LENGTH;
                string phone = line.Substring(index, PHONE_LENGTH).Trim(); index += PHONE_LENGTH;

               
                string activeS = line.Substring(index, ISACTIVE_LENGTH); index += ISACTIVE_LENGTH;

                string bookIdS = "0";
                if (line.Length >= index + BOOKID_LENGTH)
                {
                    bookIdS = line.Substring(index, BOOKID_LENGTH);
                }

                return new Member
                {
                    Id = int.TryParse(idS, out var i) ? i : 0,
                    FullName = name,
                    Email = email,
                    PhoneNumber = phone,
                    IsActive = activeS == "1",
                    CurrentBookId = int.TryParse(bookIdS, out var bId) ? bId : 0,
                    MembershipDate = DateTime.Now
                };
            }
            catch
            {
                return null;
            }
        }

        private string BuildLine(Member m)
        {
            string id = m.Id.ToString().PadLeft(ID_LENGTH, '0');
            string name = (m.FullName ?? "").PadRight(NAME_LENGTH).Substring(0, NAME_LENGTH);
            string email = (m.Email ?? "").PadRight(EMAIL_LENGTH).Substring(0, EMAIL_LENGTH);
            string phone = (m.PhoneNumber ?? "").PadRight(PHONE_LENGTH).Substring(0, PHONE_LENGTH);
            string active = m.IsActive ? "1" : "0";
            string bookId = m.CurrentBookId.ToString().PadLeft(BOOKID_LENGTH, '0');

            return string.Concat(id, name, email, phone, active, bookId);
        }
    }
}