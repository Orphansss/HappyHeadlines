namespace CommentService.Services;

static class CacheKeys
{
    public const string Prefix = "hh:v1";

    public static string CommentsByArticle(int articleId) => $"{Prefix}:comments:article:{articleId}";
    public const string CommentsLru = $"{Prefix}:comments:lru"; // Sorted Set of articleIds
}
