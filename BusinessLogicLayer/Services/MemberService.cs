using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Repostories;
using DataAccessLayer.Repostories.Contracts;
using DataAccessLayerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services
{
    public class MemberService : IMemberService
    {
        private readonly MemberRepository _repo;

        public MemberService(MemberRepository repo)
        {
            _repo = repo;
        }

        public void Create(Member member)
        {
            Validate(member, true);
            _repo.Add(member);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }

        public Member Get(int id)
        {
            return _repo.GetById(id);
        }

        public List<Member> GetAll()
        {
            return _repo.GetAll();
        }

        public List<Member> Search(string keyword)
        {
            return _repo.Search(keyword);
        }

        public void Update(Member member)
        {
            Validate(member, false);
            _repo.Update(member);
        }

        private void Validate(Member m, bool isNew)
        {
            if (m == null) throw new ArgumentException("Member cannot be null.");
            if (string.IsNullOrWhiteSpace(m.FullName)) throw new ArgumentException("Full name is required.");
            if (string.IsNullOrWhiteSpace(m.Email) || !Regex.IsMatch(m.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Email is invalid.");
            if (!string.IsNullOrWhiteSpace(m.PhoneNumber) && m.PhoneNumber.Length > 15) throw new ArgumentException("Phone number too long.");
            if (m.MembershipDate == DateTime.MinValue) m.MembershipDate = DateTime.Now;
        }
    }
}
