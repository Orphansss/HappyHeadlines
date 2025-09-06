using AS_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AS_API.Data;

namespace AS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController(ArticleDbContext db) : ControllerBase
    {
        // create a new article
        // runs when a client sends POST /api/articles
        [HttpPost]
        public async Task<ActionResult<Article>> Create([FromBody] Article dto)
        {
            // check if title is missing or empty
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            // force id to 0 so EF will auto-generate it
            //dto = data transfer object (just a plain class used to send data between the client and the server)
            dto.Id = 0;

            // if PublishedAt not set, default to current time
            dto.PublishedAt = dto.PublishedAt == default ? DateTimeOffset.UtcNow : dto.PublishedAt;

            // add new article to database
            db.Articles.Add(dto);

            // save changes
            await db.SaveChangesAsync();

            // return created response with new article data
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        // get all articles
        // runs when a client sends GET /api/articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Article>>> GetAll()
            // fetch all articles ordered by newest first
            => Ok(await db.Articles.OrderByDescending(a => a.PublishedAt).ToListAsync());

        // get one article by id
        // runs when a client sends GET /api/articles/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Article>> GetById(int id)
        {
            // look for article in database
            var item = await db.Articles.FindAsync(id);

            // if not found return 404, else return the article
            return item is null ? NotFound() : Ok(item);
        }

        // update an article
        // runs when a client sends PUT /api/articles/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Article>> Update(int id, [FromBody] Article dto)
        {
            // find article in database
            var item = await db.Articles.FindAsync(id);

            // if not found return 404
            if (item is null) return NotFound();

            // update title if provided, else keep old title
            item.Title = dto.Title ?? item.Title;

            // replace body
            item.Body = dto.Body;

            // keep PublishedAt unchanged

            // save changes
            await db.SaveChangesAsync();

            // return updated article
            return Ok(item);
        }

        // delete an article
        // runs when a client sends DELETE /api/articles/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // find article in database
            var item = await db.Articles.FindAsync(id);

            // if not found return 404
            if (item is null) return NotFound();

            // remove article
            db.Articles.Remove(item);

            // save changes
            await db.SaveChangesAsync();

            // return no content response
            return NoContent();
        }
    }
}
