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
    public class UserGoogleTokensController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserGoogleTokensController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: UserGoogleTokens
        public async Task<IActionResult> Index()
        {
            return View(await _context.UserGoogleTokens.ToListAsync());
        }

        // GET: UserGoogleTokens/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userGoogleToken = await _context.UserGoogleTokens
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userGoogleToken == null)
            {
                return NotFound();
            }

            return View(userGoogleToken);
        }

        // GET: UserGoogleTokens/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UserGoogleTokens/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,AccessToken,RefreshToken,ExpiresAt,CreatedAt,UpdatedAt")] UserGoogleToken userGoogleToken)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userGoogleToken);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(userGoogleToken);
        }

        // GET: UserGoogleTokens/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userGoogleToken = await _context.UserGoogleTokens.FindAsync(id);
            if (userGoogleToken == null)
            {
                return NotFound();
            }
            return View(userGoogleToken);
        }

        // POST: UserGoogleTokens/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,AccessToken,RefreshToken,ExpiresAt,CreatedAt,UpdatedAt")] UserGoogleToken userGoogleToken)
        {
            if (id != userGoogleToken.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userGoogleToken);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserGoogleTokenExists(userGoogleToken.Id))
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
            return View(userGoogleToken);
        }

        // GET: UserGoogleTokens/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userGoogleToken = await _context.UserGoogleTokens
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userGoogleToken == null)
            {
                return NotFound();
            }

            return View(userGoogleToken);
        }

        // POST: UserGoogleTokens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userGoogleToken = await _context.UserGoogleTokens.FindAsync(id);
            if (userGoogleToken != null)
            {
                _context.UserGoogleTokens.Remove(userGoogleToken);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserGoogleTokenExists(int id)
        {
            return _context.UserGoogleTokens.Any(e => e.Id == id);
        }
    }
}
