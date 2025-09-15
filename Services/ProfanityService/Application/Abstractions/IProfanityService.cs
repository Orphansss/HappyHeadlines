using ProfanityService.Domain.Entities;

namespace ProfanityService.Application.Abstractions;
public interface IProfanityService
{
    Task<Profanity> CreateProfanityTerm(Profanity profanity);
    Task<IEnumerable<Profanity>> GetProfanityTerms();
    Task<IReadOnlyList<Profanity>> GetActiveProfanityTerms();
    Task<Profanity?> GetProfanityTermById(int id);
    Task<Profanity?> UpdateProfanityTerm(int id, Profanity profanity);
    // Soft Delete terms
    Task<bool> DeactivateProfanityTerm(int id);
    Task<bool> ReactivateProfanityTerm(int id);
}