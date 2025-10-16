using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2025_Project3_esbusby.Data;
using Fall2025_Project3_esbusby.Models;

namespace Fall2025_Project3_esbusby.Controllers
{
    public class ActorMoviesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActorMoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ActorMovies
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ActorMovie.Include(a => a.Actor).Include(a => a.Movie);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ActorMovies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actorMovie = await _context.ActorMovie
                .Include(a => a.Actor)
                .Include(a => a.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actorMovie == null)
            {
                return NotFound();
            }

            return View(actorMovie);
        }

        // GET: ActorMovies/Create
        public IActionResult Create()
        {
            ViewData["ActorId"] = new SelectList(_context.Actor, "Id", "Name");
            ViewData["MovieId"] = new SelectList(_context.Movie, "Id", "Title");
            return View();
        }

        // POST: ActorMovies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ActorId,MovieId")] ActorMovie actorMovie)
        {
            // Check for duplicate relationship
            var existingRelationship = await _context.ActorMovie
                .FirstOrDefaultAsync(am => am.ActorId == actorMovie.ActorId && am.MovieId == actorMovie.MovieId);

            if (existingRelationship != null)
            {
                ModelState.AddModelError("", "This actor is already assigned to this movie.");
                ViewData["ActorId"] = new SelectList(_context.Actor, "Id", "Name", actorMovie.ActorId);
                ViewData["MovieId"] = new SelectList(_context.Movie, "Id", "Title", actorMovie.MovieId);
                return View(actorMovie);
            }

            if (ModelState.IsValid)
            {
                _context.Add(actorMovie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ActorId"] = new SelectList(_context.Actor, "Id", "Name", actorMovie.ActorId);
            ViewData["MovieId"] = new SelectList(_context.Movie, "Id", "Title", actorMovie.MovieId);
            return View(actorMovie);
        }

        // GET: ActorMovies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actorMovie = await _context.ActorMovie.FindAsync(id);
            if (actorMovie == null)
            {
                return NotFound();
            }
            ViewData["ActorId"] = new SelectList(_context.Actor, "Id", "Name", actorMovie.ActorId);
            ViewData["MovieId"] = new SelectList(_context.Movie, "Id", "Title", actorMovie.MovieId);
            return View(actorMovie);
        }

        // POST: ActorMovies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ActorId,MovieId")] ActorMovie actorMovie)
        {
            if (id != actorMovie.Id)
            {
                return NotFound();
            }

            // Check for duplicate relationship (excluding current record)
            var existingRelationship = await _context.ActorMovie
                .FirstOrDefaultAsync(am => am.ActorId == actorMovie.ActorId
                    && am.MovieId == actorMovie.MovieId
                    && am.Id != actorMovie.Id);

            if (existingRelationship != null)
            {
                ModelState.AddModelError("", "This actor is already assigned to this movie.");
                ViewData["ActorId"] = new SelectList(_context.Actor, "Id", "Name", actorMovie.ActorId);
                ViewData["MovieId"] = new SelectList(_context.Movie, "Id", "Title", actorMovie.MovieId);
                return View(actorMovie);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actorMovie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorMovieExists(actorMovie.Id))
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
            ViewData["ActorId"] = new SelectList(_context.Actor, "Id", "Name", actorMovie.ActorId);
            ViewData["MovieId"] = new SelectList(_context.Movie, "Id", "Title", actorMovie.MovieId);
            return View(actorMovie);
        }

        // GET: ActorMovies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actorMovie = await _context.ActorMovie
                .Include(a => a.Actor)
                .Include(a => a.Movie)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actorMovie == null)
            {
                return NotFound();
            }

            return View(actorMovie);
        }

        // POST: ActorMovies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actorMovie = await _context.ActorMovie.FindAsync(id);
            if (actorMovie != null)
            {
                _context.ActorMovie.Remove(actorMovie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorMovieExists(int id)
        {
            return _context.ActorMovie.Any(e => e.Id == id);
        }
    }
}