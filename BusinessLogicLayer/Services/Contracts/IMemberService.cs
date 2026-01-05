using DataAccessLayerModels;

namespace BusinessLogicLayer.Services.Contracts
{
    public interface IMemberService
    {
        void Create(Member member);
        Member Get(int id);
        List<Member> GetAll();
        void Update(Member member);
        void Delete(int id);
        List<Member> Search(string keyword);
    }
}
