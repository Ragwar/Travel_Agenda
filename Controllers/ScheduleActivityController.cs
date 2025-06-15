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
    public class ScheduleActivityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleActivityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ScheduleActivity
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Day_Activities.Include(s => s.Schedule);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ScheduleActivity/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ScheduleActivity = await _context.Day_Activities
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(m => m.ScheduleActivityId == id);
            if (ScheduleActivity == null)
            {
                return NotFound();
            }

            return View(ScheduleActivity);
        }

        // GET: ScheduleActivity/Create
        public IActionResult Create()
        {
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "ScheduleId");
            return View();
        }

        // POST: ScheduleActivity/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduleActivityId,StartHour,EndHour,StartMinute,EndMinute,StartDate,EndDate,AddInfo,Name,PlaceId,Type,Available,ScheduleId")] ScheduleActivity ScheduleActivity)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ScheduleActivity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "ScheduleId", ScheduleActivity.ScheduleId);
            return View(ScheduleActivity);
        }

        // GET: ScheduleActivity/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ScheduleActivity = await _context.Day_Activities.FindAsync(id);
            if (ScheduleActivity == null)
            {
                return NotFound();
            }
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "ScheduleId", ScheduleActivity.ScheduleId);
            return View(ScheduleActivity);
        }

        // POST: ScheduleActivity/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ScheduleActivityId,StartHour,EndHour,StartMinute,EndMinute,StartDate,EndDate,AddInfo,Name,PlaceId,Type,Available,ScheduleId")] ScheduleActivity ScheduleActivity)
        {
            if (id != ScheduleActivity.ScheduleActivityId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ScheduleActivity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleActivityExists(ScheduleActivity.ScheduleActivityId))
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
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "ScheduleId", ScheduleActivity.ScheduleId);
            return View(ScheduleActivity);
        }

        // GET: ScheduleActivity/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ScheduleActivity = await _context.Day_Activities
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(m => m.ScheduleActivityId == id);
            if (ScheduleActivity == null)
            {
                return NotFound();
            }

            return View(ScheduleActivity);
        }

        // POST: ScheduleActivity/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ScheduleActivity = await _context.Day_Activities.FindAsync(id);
            if (ScheduleActivity != null)
            {
                _context.Day_Activities.Remove(ScheduleActivity);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleActivityExists(int id)
        {
            return _context.Day_Activities.Any(e => e.ScheduleActivityId == id);
        }
    }
}
