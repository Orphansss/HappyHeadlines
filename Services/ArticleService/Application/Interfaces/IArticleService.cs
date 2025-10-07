using ArticleService.Api.Contracts.Dtos;

namespace ArticleService.Application.Interfaces;

public interface IArticleService
{
    Task<ArticleResponse> CreateAsync(CreateArticleRequest input, CancellationToken ct = default);
    Task<IReadOnlyList<ArticleResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ArticleResponse>> GetLatestAsync(int count, CancellationToken ct = default);
    Task<ArticleResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ArticleResponse?> UpdateAsync(int id, UpdateArticleRequest input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
