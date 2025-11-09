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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Fall2025_Project3_esbusby.Controllers
{
    public record Tweet(string Username, string Text);
    public record Tweets(Tweet[] Items);

    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ActorsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Actors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actor.ToListAsync());
        }

        // GET: Actors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .Include(a => a.ActorMovies!)
                    .ThenInclude(am => am.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (actor == null)
            {
                return NotFound();
            }

            var apiEndpoint = new Uri(_configuration["ai_endpoint"]!);
            var apiCredential = new ApiKeyCredential(_configuration["api_key"]!);
            var aiDeployment = _configuration["ai_deployment_name"]!;

            ChatClient client = new AzureOpenAIClient(apiEndpoint, apiCredential).GetChatClient(aiDeployment);

            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            JsonNode schema = options.GetJsonSchemaAsNode(typeof(Tweets), new()
            {
                TreatNullObliviousAsNonNullable = true,
            });

            var chatCompletionOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("XTwitterApiJson", BinaryData.FromString(schema.ToString()), jsonSchemaIsStrict: true),
            };
            var messages = new ChatMessage[]
            {
                new SystemChatMessage($"You represent the X/Twitter social media platform API that returns JSON data."),
                new UserChatMessage($"Generate 5 tweets from a variety of users about the actor {actor.Name}.")
            };
            ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages, chatCompletionOptions);

            string jsonString = result.Value.Content.FirstOrDefault()?.Text ?? @"{""Items"":[]}";
            Tweets tweets = JsonSerializer.Deserialize<Tweets>(jsonString) ?? new Tweets(Array.Empty<Tweet>());

            var analyzer = new SentimentIntensityAnalyzer();
            double sentimentTotal = 0;
            var tweetsList = new List<(string username, string text, SentimentAnalysisResults sentiment)>();

            foreach (var tweet in tweets.Items)
            {
                SentimentAnalysisResults sentiment = analyzer.PolarityScores(tweet.Text);
                sentimentTotal += sentiment.Compound;
                tweetsList.Add((tweet.Username, tweet.Text, sentiment));
            }

            double sentimentAverage = tweets.Items.Length > 0 ? sentimentTotal / tweets.Items.Length : 0;

            ViewBag.Tweets = tweetsList;
            ViewBag.AverageSentiment = sentimentAverage;

            return View(actor);
        }

        // GET: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Gender,Age,ImdbLink")] Actor actor, IFormFile? Photo)
        {
            var existingActor = await _context.Actor
                .FirstOrDefaultAsync(a => a.Name == actor.Name && a.Age == actor.Age);

            if (existingActor != null)
            {
                ModelState.AddModelError("Name", "An actor with this name and age already exists.");
                return View(actor);
            }

            if (ModelState.IsValid)
            {
                if (Photo != null && Photo.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Photo.CopyToAsync(memoryStream);
                        actor.Photo = memoryStream.ToArray();
                    }
                }
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        // GET: Actors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Gender,Age,ImdbLink")] Actor actor, IFormFile? Photo)
        {
            if (id != actor.Id)
            {
                return NotFound();
            }

            var existingActor = await _context.Actor
                .FirstOrDefaultAsync(a => a.Name == actor.Name
                    && a.Age == actor.Age
                    && a.Id != actor.Id);

            if (existingActor != null)
            {
                ModelState.AddModelError("Name", "An actor with this name and age already exists.");
                return View(actor);
            }

            ModelState.Remove("Photo");

            if (ModelState.IsValid)
            {
                try
                {
                    if (Photo != null && Photo.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await Photo.CopyToAsync(memoryStream);
                            actor.Photo = memoryStream.ToArray();
                        }
                    }
                    else
                    {
                        // Keep existing photo if no new one is uploaded
                        var actorForPhoto = await _context.Actor.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                        if (actorForPhoto != null)
                        {
                            actor.Photo = actorForPhoto.Photo;
                        }
                    }

                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.Id))
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
            return View(actor);
        }

        // GET: Actors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return _context.Actor.Any(e => e.Id == id);
        }
    }
}