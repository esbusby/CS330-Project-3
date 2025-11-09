using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2025_Project3_esbusby.Data;
using Fall2025_Project3_esbusby.Models;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using VaderSharp2;

namespace Fall2025_Project3_esbusby.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public MoviesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .Include(m => m.ActorMovies!)
                    .ThenInclude(am => am.Actor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            var apiEndpoint = new Uri(_configuration["ai_endpoint"]!);
            var apiCredential = new ApiKeyCredential(_configuration["api_key"]!);
            var aiDeployment = _configuration["ai_deployment_name"]!;

            ChatClient client = new AzureOpenAIClient(apiEndpoint, apiCredential).GetChatClient(aiDeployment);

            string[] personas = { "is harsh", "loves romance", "loves comedy", "loves thrillers", "loves fantasy", "appreciates cinematography", "enjoys storytelling" };
            var messages = new ChatMessage[]
            {
                new SystemChatMessage($"You represent a group of 3 film critics who have the following personalities: {string.Join(",", personas)}. When you receive a question, respond as exactly 3 members of the group with each response separated by a '|' character, but don't indicate which member you are. IMPORTANT: You must provide exactly 3 reviews separated by the '|' character."),
                new UserChatMessage($"How would you rate the movie {movie.Title} released in {movie.YearOfRelease} out of 10 in 150 words or less? Give me exactly 3 reviews separated by '|'.")
            };
            ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages);
            string responseText = result.Value.Content[0].Text;

            string[] reviews = responseText.Split('|')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (reviews.Length < 3)
            {
                var tempList = reviews.ToList();
                while (tempList.Count < 3)
                {
                    tempList.Add($"Review of {movie.Title}: A compelling film worth watching.");
                }
                reviews = tempList.ToArray();
            }
            else if (reviews.Length > 3)
            {
                reviews = reviews.Take(3).ToArray();
            }

            var analyzer = new SentimentIntensityAnalyzer();
            double sentimentTotal = 0;
            var reviewsList = new List<(string review, SentimentAnalysisResults sentiment)>();

            for (int i = 0; i < reviews.Length; i++)
            {
                string review = reviews[i];
                SentimentAnalysisResults sentiment = analyzer.PolarityScores(review);
                sentimentTotal += sentiment.Compound;

                reviewsList.Add((review, sentiment));
            }

            double sentimentAverage = sentimentTotal / reviews.Length;

            ViewBag.Reviews = reviewsList;
            ViewBag.AverageSentiment = sentimentAverage;

            return View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,ImdbLink,Genre,YearOfRelease")] Movie movie, IFormFile? Poster)
        {

            var existingMovie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Title == movie.Title && m.YearOfRelease == movie.YearOfRelease);

            if (existingMovie != null)
            {
                ModelState.AddModelError("Title", "A movie with this title and year already exists.");
                return View(movie);
            }

            if (ModelState.IsValid)
            {
                // Handle the poster upload
                if (Poster != null && Poster.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Poster.CopyToAsync(memoryStream);
                        movie.Poster = memoryStream.ToArray();
                    }
                }
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ImdbLink,Genre,YearOfRelease")] Movie movie, IFormFile? Poster)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            var existingMovie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Title == movie.Title
                    && m.YearOfRelease == movie.YearOfRelease
                    && m.Id != movie.Id);

            if (existingMovie != null)
            {
                ModelState.AddModelError("Title", "A movie with this title and year already exists.");
                return View(movie);
            }

            ModelState.Remove("Poster");

            if (ModelState.IsValid)
            {
                try
                {
                    if (Poster != null && Poster.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await Poster.CopyToAsync(memoryStream);
                            movie.Poster = memoryStream.ToArray();
                        }
                    }
                    else
                    {
                        // Keep existing poster if no new one is uploaded
                        var movieForPoster = await _context.Movie.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                        if (movieForPoster != null)
                        {
                            movie.Poster = movieForPoster.Poster;
                        }
                    }

                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }
    }
}