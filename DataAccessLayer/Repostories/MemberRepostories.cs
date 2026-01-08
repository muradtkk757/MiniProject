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
    public class MemberRepository : IRepository<Member>
    {
        private readonly string _file = Data.Data.MembersFile;

        private const int ID_LENGTH = 5;
        private const int FULLNAME_LENGTH = 30;
        private const int EMAIL_LENGTH = 40;
        private const int PHONE_LENGTH = 15;
        private const int DATE_LENGTH = 8;
        private const int ISACTIVE_LENGTH = 1;

        public MemberRepository()
        {
            Data.Data.EnsureDataFiles();
        }

        public void Add(Member entity)
        {
            var all = GetAll();
            int nextId = (all.Any() ? all.Max(m => m.Id) : 0) + 1;
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
                throw new Exception("Member not found.");
        }

        public List<Member> GetAll()
        {
            var result = new List<Member>();
            foreach (var line in File.ReadAllLines(_file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                result.Add(ParseLine(line));
            }
            return result;
        }

        public Member GetById(int id)
        {
            var line = File.ReadAllLines(_file).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && ParseIdFromLine(l) == id);
            if (line == null) return null;
            return ParseLine(line);
        }

        public List<Member> Search(string keyword)
        {
            keyword = (keyword ?? string.Empty).ToLower();
            return GetAll().Where(m =>
                (!string.IsNullOrEmpty(m.FullName) && m.FullName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(m.Email) && m.Email.ToLower().Contains(keyword))
            ).ToList();
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
            else
                throw new Exception("Member not found.");
        }

        private int ParseIdFromLine(string line)
        {
            var idStr = line.Substring(0, ID_LENGTH);
            if (int.TryParse(idStr, out int id)) return id;
            return 0;
        }

        private Member ParseLine(string line)
        {
            try
            {
                int index = 0;
                string idS = line.Substring(index, ID_LENGTH); index += ID_LENGTH;
                string name = line.Substring(index, FULLNAME_LENGTH); index += FULLNAME_LENGTH;
                string email = line.Substring(index, EMAIL_LENGTH); index += EMAIL_LENGTH;
                string phone = line.Substring(index, PHONE_LENGTH); index += PHONE_LENGTH;
                string dateS = line.Substring(index, DATE_LENGTH); index += DATE_LENGTH;
                string activeS = line.Substring(index, ISACTIVE_LENGTH);

                DateTime.TryParseExact(dateS, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime membershipDate);

                return new Member
                {
                    Id = int.TryParse(idS, out var i) ? i : 0,
                    FullName = name.Trim(),
                    Email = email.Trim(),
                    PhoneNumber = phone.Trim(),
                    MembershipDate = membershipDate == default ? DateTime.MinValue : membershipDate,
                    IsActive = activeS == "1"
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse member line. " + ex.Message);
            }
        }

        private string BuildLine(Member m)
        {
            string id = m.Id.ToString().PadLeft(ID_LENGTH, '0');
            string name = (m.FullName ?? string.Empty).PadRight(FULLNAME_LENGTH).Substring(0, FULLNAME_LENGTH);
            string email = (m.Email ?? string.Empty).PadRight(EMAIL_LENGTH).Substring(0, EMAIL_LENGTH);
            string phone = (m.PhoneNumber ?? string.Empty).PadRight(PHONE_LENGTH).Substring(0, PHONE_LENGTH);
            string date = (m.MembershipDate == DateTime.MinValue) ? DateTime.Now.ToString("yyyyMMdd") : m.MembershipDate.ToString("yyyyMMdd");
            date = date.PadLeft(DATE_LENGTH, '0').Substring(0, DATE_LENGTH);
            string active = m.IsActive ? "1" : "0";

            return string.Concat(id, name, email, phone, date, active);
        }
    }
}