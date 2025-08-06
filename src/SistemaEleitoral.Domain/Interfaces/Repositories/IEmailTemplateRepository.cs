using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces.Repositories
{
    public interface IEmailTemplateRepository : IBaseRepository<EmailTemplate>
    {
        Task<EmailTemplate?> GetByNameAsync(string name);
        Task<List<EmailTemplate>> GetByCategoryAsync(string category);
        Task<List<EmailTemplate>> GetActiveTemplatesAsync();
    }
}