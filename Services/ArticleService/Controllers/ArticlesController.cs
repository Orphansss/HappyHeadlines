using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArticleService.Data;
using ArticleService.Models;

namespace ArticleService.Controllers
{
     [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController(ArticleDbContext db) : ControllerBase
    {
        // CREATE a new article
        // Runs when a client sends POST /api/articles
        [HttpPost]
        public async Task<ActionResult<Article>> Create([FromBody] Article article)
        {
            // check if title is missing or empty
            if (string.IsNullOrWhiteSpace(article.Title))
                return BadRequest("Title is required.");

            // set Id = 0 so EF will auto-generate a new one
            article.Id = 0;

            // if PublishedAt is not set, use current time
            if (article.PublishedAt == default)
                article.PublishedAt = DateTimeOffset.UtcNow;

            // add article to database
            db.Articles.Add(article);

            // save changes
            await db.SaveChangesAsync();

            // return "201 Created" with link to new article
            return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
        }

        // READ all articles
        // Runs when a client sends GET /api/articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Article>>> GetAll()
        {
            // fetch all articles ordered by newest first
            var items = await db.Articles
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return Ok(items);
        }

        // READ one article by id
        // Runs when a client sends GET /api/articles/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Article>> GetById(int id)
        {
            // look for article in database
            var item = await db.Articles.FindAsync(id);

            // if not found return 404, else return the article
            return item is null ? NotFound() : Ok(item);
        }

        // UPDATE an article
        // Runs when a client sends PUT /api/articles/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Article>> Update(int id, [FromBody] Article article)
        {
            // find article in database
            var existing = await db.Articles.FindAsync(id);

            // if not found return 404
            if (existing is null) return NotFound();

            // update title if provided, else keep old one
            if (!string.IsNullOrWhiteSpace(article.Title))
                existing.Title = article.Title;

            // update body
            existing.Body = article.Body;

            // PublishedAt stays the same

            // save changes
            await db.SaveChangesAsync();

            // return updated article
            return Ok(existing);
        }

        // DELETE an article
        // Runs when a client sends DELETE /api/articles/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // find article in database
            var item = await db.Articles.FindAsync(id);

            // if not found return 404
            if (item is null) return NotFound();

            // remove article from database
            db.Articles.Remove(item);

            // save changes
            await db.SaveChangesAsync();

            // return 204 No Content
            return NoContent();
        }
    }
}
