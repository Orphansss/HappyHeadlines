using ProfanityService.Domain.Entities;

namespace ProfanityService.Application.Abstractions;
public interface IProfanityService
{
    Task<Profanity> CreateProfanityTerm(Profanity profanity);
    Task<IEnumerable<Profanity>> GetProfanityTerms();
    Task<IEnumerable<Profanity>> GetActiveProfanityTerms(bool includeInactive = false);
    Task<Profanity?> GetProfanityTerm(int id);
    Task<Profanity?> UpdateProfanityTerm(int id, Profanity profanity);
    // Soft Delete terms
    Task<Boolean> DeactivateProfanityTerm(int id);
    Task<Boolean> ReactivateProfanityTerm(int id);
}