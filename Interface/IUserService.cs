using MyProject.Areas.Admin.Models;

namespace MyProject.Interface
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task Add(User user);
        Task Update(User user);
        Task Delete(int id);
    }
}
