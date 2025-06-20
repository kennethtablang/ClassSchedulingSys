using ClassSchedulingSys.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Dean,SuperAdmin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("class/{id}/schedule-pdf")]
        public async Task<IActionResult> ExportClassSchedule(int id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.ClassId == id)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(s => s.SchoolYear)
                .Include(s => s.Class)
                .ToListAsync();

            if (!schedules.Any()) return NotFound("No schedule found for this class.");

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text($"Schedule for Class: {schedules.First().Class?.Name}").FontSize(18).SemiBold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Subject").SemiBold();
                            header.Cell().Text("Faculty").SemiBold();
                            header.Cell().Text("Day").SemiBold();
                            header.Cell().Text("Time").SemiBold();
                            header.Cell().Text("Room").SemiBold();
                        });

                        foreach (var s in schedules.OrderBy(s => s.Day).ThenBy(s => s.StartTime))
                        {
                            table.Cell().Text(s.Subject?.Code + " - " + s.Subject?.Title);
                            table.Cell().Text(s.Faculty?.FullName);
                            table.Cell().Text(s.Day.ToString());
                            table.Cell().Text($"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}");
                            table.Cell().Text(s.Room?.Name);
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"ClassSchedule_{schedules.First().Class?.Name}.pdf");
        }

        [HttpGet("faculty/{id}/schedule-pdf")]
        public async Task<IActionResult> ExportFacultySchedule(string id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.FacultyId == id)
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(s => s.SchoolYear)
                .Include(s => s.Class)
                .Include(s => s.Faculty)
                .ToListAsync();

            if (!schedules.Any()) return NotFound("No schedule found for this faculty member.");

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text($"Schedule for Faculty: {schedules.First().Faculty?.FullName}").FontSize(18).SemiBold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Subject").SemiBold();
                            header.Cell().Text("Day").SemiBold();
                            header.Cell().Text("Time").SemiBold();
                            header.Cell().Text("Room").SemiBold();
                            header.Cell().Text("Class").SemiBold();
                        });

                        foreach (var s in schedules.OrderBy(s => s.Day).ThenBy(s => s.StartTime))
                        {
                            table.Cell().Text(s.Subject?.Code + " - " + s.Subject?.Title);
                            table.Cell().Text(s.Day.ToString());
                            table.Cell().Text($"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}");
                            table.Cell().Text(s.Room?.Name);
                            table.Cell().Text(s.Class?.Name);
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"FacultySchedule_{schedules.First().Faculty?.UserName}.pdf");
        }

        [HttpGet("room/{id}/schedule-pdf")]
        public async Task<IActionResult> ExportRoomSchedule(int id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.RoomId == id)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Semester)
                    .ThenInclude(s => s.SchoolYear)
                .Include(s => s.Class)
                .Include(s => s.Room)
                .ToListAsync();

            if (!schedules.Any()) return NotFound("No schedule found for this room.");

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Header().Text($"Schedule for Room: {schedules.First().Room?.Name}").FontSize(18).SemiBold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Subject").SemiBold();
                            header.Cell().Text("Day").SemiBold();
                            header.Cell().Text("Time").SemiBold();
                            header.Cell().Text("Faculty").SemiBold();
                            header.Cell().Text("Class").SemiBold();
                        });

                        foreach (var s in schedules.OrderBy(s => s.Day).ThenBy(s => s.StartTime))
                        {
                            table.Cell().Text(s.Subject?.Code + " - " + s.Subject?.Title);
                            table.Cell().Text(s.Day.ToString());
                            table.Cell().Text($"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}");
                            table.Cell().Text(s.Faculty?.FullName);
                            table.Cell().Text(s.Class?.Name);
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"RoomSchedule_{schedules.First().Room?.Name}.pdf");
        }
    }
}
