using Microsoft.EntityFrameworkCore;
using ProfanityService.Application.Abstractions;
using ProfanityService.Domain.Entities;
using ProfanityService.Infrastructure.Data;

namespace ProfanityService.Applications;
public class ProfanityService : IProfanityService
{
    private readonly ProfanityDbContext _db;

    public ProfanityService(ProfanityDbContext db)
    {
        _db = db;
    }

    public async Task<Profanity> CreateProfanityTerm(Profanity profanity)
    {
        _db.Profanities.Add(profanity);
        await _db.SaveChangesAsync();
        return profanity;
    }

    public async Task<IReadOnlyList<Profanity>> GetActiveProfanityTerms()
    {
        return await _db.Profanities.AsNoTracking().Where(p => p.IsActive).ToListAsync();
    }

    public async Task<Profanity?> GetProfanityTermById(int id)
    {
        return await _db.Profanities.FindAsync(id);
    }

    public async Task<IEnumerable<Profanity>> GetProfanityTerms()
    {
        return await _db.Profanities.ToListAsync();
    }

    public async Task<Profanity?> UpdateProfanityTerm(int id, Profanity profanity)
    {
        var existing = await _db.Profanities.FindAsync(id);
        if (existing == null) return null;

        existing.Term = profanity.Term;
        existing.Serverity = profanity.Serverity;
        await _db.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeactivateProfanityTerm(int id)
    {
        var existing = await _db.Profanities.FindAsync(id);
        if (existing == null) return false;
        if (!existing.IsActive) return true;

        existing.IsActive = false;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReactivateProfanityTerm(int id)
    {
        var existing = await _db.Profanities.FindAsync(id);
        if (existing == null) return false;
        if (existing.IsActive) return true;

        existing.IsActive = true;
        await _db.SaveChangesAsync();

        return true;
    }
}
