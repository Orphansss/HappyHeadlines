using DraftService.Models;

namespace DraftService.Interfaces
{

        public interface IDraftService
        {
            Task<Draft> CreateDraft(Draft draft);
            Task<IEnumerable<Draft>> GetDrafts();
            Task<Draft?> GetDraftById(int id);
            Task<Draft?> UpdateDraft(int id, Draft draft);
            Task<bool> DeleteDraft(int id);
        }
    
}
