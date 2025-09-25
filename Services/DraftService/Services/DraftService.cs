using DraftService.Interfaces;
using DraftService.Models;
using DraftService.Data;
using Microsoft.EntityFrameworkCore;


namespace DraftService.Services
{

    public class DraftService : IDraftService
    {
        private readonly DraftDbContext _db;

        public DraftService(DraftDbContext db)
        {
            _db = db;
        }

        public async Task<Draft> CreateDraft(Draft draft)
        {
            _db.Drafts.Add(draft);
            await _db.SaveChangesAsync();
            return draft;
        }

        public async Task<IEnumerable<Draft>> GetDrafts()
        {
            return await _db.Drafts.ToListAsync();
        }

        public async Task<Draft?> GetDraftById(int id)
        {
            return await _db.Drafts.FindAsync(id);
        }

        public async Task<List<Draft>> GetAllDrafts()
        {
            return await _db.Drafts.ToListAsync();
        }

        public async Task<Draft?> UpdateDraft(int id, Draft draft)
        {
            var existingDraft = await _db.Drafts.FindAsync(id);
            if (existingDraft == null)
            {
                return null;
            }
            existingDraft.ArticleId = draft.ArticleId;
            existingDraft.Title = draft.Title;
            existingDraft.Body = draft.Body;
            existingDraft.Author = draft.Author;
            existingDraft.LastModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existingDraft;
        }

        public async Task<bool> DeleteDraft(int id)
        {
            var draft = await _db.Drafts.FindAsync(id);
            if (draft == null)
            {
                return false;
            }
            _db.Drafts.Remove(draft);
            await _db.SaveChangesAsync();
            return true;
        }
    }

}