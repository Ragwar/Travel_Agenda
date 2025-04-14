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
    public class Schedule_ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public Schedule_ActivityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedule_Activity
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Day_Activities.Include(s => s.Schedule);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Schedule_Activity/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule_Activity = await _context.Day_Activities
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(m => m.Schedule_Activity_Id == id);
            if (schedule_Activity == null)
            {
                return NotFound();
            }

            return View(schedule_Activity);
        }

        // GET: Schedule_Activity/Create
        public IActionResult Create()
        {
            ViewData["Activity_Id"] = new SelectList(_context.Activities, "Activity_Id", "Activity_Id");
            ViewData["Schedule_Id"] = new SelectList(_context.Schedules, "Schedule_Id", "Schedule_Id");
            return View();
        }

        // POST: Schedule_Activity/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Schedule_Activity_Id,Start_Hour,End_Hour,Start_Minute,End_Minute,Start_Date,End_Date,Add_Info,Name,Place_Id,Type,Available,Schedule_Id,Activity_Id")] Schedule_Activity schedule_Activity)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schedule_Activity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Schedule_Id"] = new SelectList(_context.Schedules, "Schedule_Id", "Schedule_Id", schedule_Activity.Schedule_Id);
            return View(schedule_Activity);
        }

        // GET: Schedule_Activity/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule_Activity = await _context.Day_Activities.FindAsync(id);
            if (schedule_Activity == null)
            {
                return NotFound();
            }

            ViewData["Schedule_Id"] = new SelectList(_context.Schedules, "Schedule_Id", "Schedule_Id", schedule_Activity.Schedule_Id);
            return View(schedule_Activity);
        }

        // POST: Schedule_Activity/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Schedule_Activity_Id,Start_Hour,End_Hour,Start_Minute,End_Minute,Start_Date,End_Date,Add_Info,Name,Place_Id,Type,Available,Schedule_Id,Activity_Id")] Schedule_Activity schedule_Activity)
        {
            if (id != schedule_Activity.Schedule_Activity_Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule_Activity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Schedule_ActivityExists(schedule_Activity.Schedule_Activity_Id))
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
            ViewData["Schedule_Id"] = new SelectList(_context.Schedules, "Schedule_Id", "Schedule_Id", schedule_Activity.Schedule_Id);
            return View(schedule_Activity);
        }

        // GET: Schedule_Activity/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule_Activity = await _context.Day_Activities
                .Include(s => s.Schedule)
                .FirstOrDefaultAsync(m => m.Schedule_Activity_Id == id);
            if (schedule_Activity == null)
            {
                return NotFound();
            }

            return View(schedule_Activity);
        }

        // POST: Schedule_Activity/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule_Activity = await _context.Day_Activities.FindAsync(id);
            if (schedule_Activity != null)
            {
                _context.Day_Activities.Remove(schedule_Activity);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Schedule_ActivityExists(int id)
        {
            return _context.Day_Activities.Any(e => e.Schedule_Activity_Id == id);
        }
    }
}
