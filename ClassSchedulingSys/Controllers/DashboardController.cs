using ClassSchedulingSys.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var subjectCount = await _context.Subjects.CountAsync();
            var facultyCount = await _context.Users.CountAsync();
            var roomCount = await _context.Rooms.CountAsync();
            var classCount = await _context.Classes.CountAsync();
            var scheduleCount = await _context.Schedules.CountAsync();

            var currentSemester = await _context.Semesters
                .Include(s => s.SchoolYear)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                TotalSubjects = subjectCount,
                TotalFaculty = facultyCount,
                TotalRooms = roomCount,
                TotalClasses = classCount,
                TotalSchedules = scheduleCount,
                CurrentSemester = currentSemester?.Name,
                CurrentSchoolYear = currentSemester?.SchoolYear?.StartYear
            });
        }

        [HttpGet("today-classes")]
        public async Task<IActionResult> GetTodayClasses()
        {
            var today = DateTime.Today.DayOfWeek;
            var now = DateTime.Now.TimeOfDay;

            var schedules = await _context.Schedules
                .Where(s => s.Day == today && s.EndTime > now)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Class)
                .ToListAsync();

            return Ok(schedules.Select(s => new
            {
                s.Id,
                Subject = s.Subject?.Code + " - " + s.Subject?.Title,
                Faculty = s.Faculty?.FullName,
                Room = s.Room?.Name,
                Class = s.Class?.Name,
                s.Day,
                s.StartTime,
                s.EndTime
            }));
        }

        [HttpGet("most-booked-room")]
        public async Task<IActionResult> GetMostBookedRoom()
        {
            var room = await _context.Schedules
                .GroupBy(s => s.RoomId)
                .Select(g => new { RoomId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync();

            if (room == null) return NotFound();

            var roomDetails = await _context.Rooms.FindAsync(room.RoomId);
            return Ok(new { Room = roomDetails?.Name, TotalSchedules = room.Count });
        }

        [HttpGet("grouped-by-day")]
        public async Task<IActionResult> GetSchedulesGroupedByDay()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Class)
                .ToListAsync();

            var grouped = schedules
                .GroupBy(s => s.Day)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Day = g.Key.ToString(),
                    Schedules = g.Select(s => new
                    {
                        s.Id,
                        Subject = s.Subject?.Code,
                        Class = s.Class?.Name,
                        s.StartTime,
                        s.EndTime
                    })
                });

            return Ok(grouped);
        }

        [HttpGet("calendar-view")]
        public async Task<IActionResult> GetCalendarView()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Class)
                .ToListAsync();

            var today = DateTime.Today;
            var events = schedules.Select(s => new
            {
                id = s.Id,
                title = $"{s.Subject?.Code} - {s.Class?.Name}",
                start = GetNextDateTime(s.Day, s.StartTime),
                end = GetNextDateTime(s.Day, s.EndTime),
                day = s.Day.ToString()
            });

            return Ok(events);
        }

        private DateTime GetNextDateTime(DayOfWeek day, TimeSpan time)
        {
            var today = DateTime.Today;
            int daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(daysUntil).Date + time;
        }
    }
}
