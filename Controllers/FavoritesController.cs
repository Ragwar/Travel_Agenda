using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelAgenda.Data;
using TravelAgenda.Models;

namespace TravelAgenda.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Favorites
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Reviews.Include(f => f.Activity).Include(f => f.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Favorites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favorites = await _context.Reviews
                .Include(f => f.Activity)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Favorites_Id == id);
            if (favorites == null)
            {
                return NotFound();
            }

            return View(favorites);
        }

        // GET: Favorites/Create
        public IActionResult Create()
        {
            ViewData["Activity_Id"] = new SelectList(_context.Activities, "Activity_Id", "Activity_Id");
            ViewData["User_Id"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Favorites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Favorites_Id,User_Id,Activity_Id")] Favorites favorites)
        {
            if (ModelState.IsValid)
            {
                _context.Add(favorites);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Activity_Id"] = new SelectList(_context.Activities, "Activity_Id", "Activity_Id", favorites.Activity_Id);
            ViewData["User_Id"] = new SelectList(_context.Users, "Id", "Id", favorites.User_Id);
            return View(favorites);
        }

        // GET: Favorites/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favorites = await _context.Reviews.FindAsync(id);
            if (favorites == null)
            {
                return NotFound();
            }
            ViewData["Activity_Id"] = new SelectList(_context.Activities, "Activity_Id", "Activity_Id", favorites.Activity_Id);
            ViewData["User_Id"] = new SelectList(_context.Users, "Id", "Id", favorites.User_Id);
            return View(favorites);
        }

        // POST: Favorites/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Favorites_Id,User_Id,Activity_Id")] Favorites favorites)
        {
            if (id != favorites.Favorites_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(favorites);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FavoritesExists(favorites.Favorites_Id))
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
            ViewData["Activity_Id"] = new SelectList(_context.Activities, "Activity_Id", "Activity_Id", favorites.Activity_Id);
            ViewData["User_Id"] = new SelectList(_context.Users, "Id", "Id", favorites.User_Id);
            return View(favorites);
        }

        // GET: Favorites/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favorites = await _context.Reviews
                .Include(f => f.Activity)
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Favorites_Id == id);
            if (favorites == null)
            {
                return NotFound();
            }

            return View(favorites);
        }

        // POST: Favorites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var favorites = await _context.Reviews.FindAsync(id);
            if (favorites != null)
            {
                _context.Reviews.Remove(favorites);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FavoritesExists(int id)
        {
            return _context.Reviews.Any(e => e.Favorites_Id == id);
        }
    }
}
