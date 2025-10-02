namespace ArticleService.Infrastructure.Messaging;

public sealed record ArticleEnvelope(
    int publicationId,
    DateTimeOffset occurredAt,
    ArticlePayload article,
    string? idempotencyKey
);

/*
EXPECTED ARTICLE PAYLOAD

{
     "publicationId": 3,
     "occurredAt": "2025-10-02T14:12:00Z",
     "article": {
       "id": 3,
       "authorId": 8,
       "title": "Region test",
       "summary": "My summary",
       "content": "My content here...",
       "region": "Asia",
       "publishedAt": "2025-10-02T14:12:00Z"
     },
     "idempotencyKey": null
   }
   
*/