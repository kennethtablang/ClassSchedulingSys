// ClassSchedulingSys/Services/FacultyScheduleGridPdfService.cs
using ClassSchedulingSys.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClassSchedulingSys.Services
{
    /// <summary>
    /// Service for generating faculty-specific schedule grid PDF files in landscape legal size
    /// </summary>
    public class FacultyScheduleGridPdfService
    {
        private readonly IWebHostEnvironment _environment;

        public FacultyScheduleGridPdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Generates a faculty schedule grid PDF in landscape legal size format
        /// </summary>
        public byte[] GenerateFacultyScheduleGridPdf(
            List<Schedule> schedules,
            string facultyName,
            string facultyEmployeeId,
            string semesterLabel,
            string schoolYearLabel)
        {
            var logoPath = Path.Combine(_environment.ContentRootPath, "Assets", "PCNL_Logo.jpg");

            // Generate time slots from 7:00 AM to 6:00 PM
            var timeSlots = GenerateTimeSlots(TimeSpan.FromHours(7), TimeSpan.FromHours(18), 30);

            // Days of the week (Monday to Saturday)
            var daysOfWeek = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Legal size landscape
                    page.Size(PageSizes.Legal.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(7));

                    // === HEADER ===
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().AlignCenter().Row(row =>
                        {
                            // Logo
                            row.AutoItem().Column(col =>
                            {
                                if (File.Exists(logoPath))
                                {
                                    col.Item().AlignMiddle().Height(40).Width(40).Image(logoPath);
                                }
                            });

                            row.AutoItem().Width(10);

                            // Title section
                            row.AutoItem().Column(col =>
                            {
                                col.Item().AlignCenter().Text("PHILIPPINE COLLEGE OF NORTHWESTERN LUZON")
                                    .Bold().FontSize(12);

                                col.Item().AlignCenter().Text("Faculty Teaching Schedule Grid")
                                    .Bold().FontSize(10);

                                col.Item().AlignCenter().Text($"{semesterLabel} • SY {schoolYearLabel}")
                                    .FontSize(8).Italic();
                            });
                        });

                        // Faculty Info Row
                        headerCol.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Faculty: {facultyName}")
                                .Bold().FontSize(8);

                            if (!string.IsNullOrWhiteSpace(facultyEmployeeId))
                            {
                                row.RelativeItem().AlignRight().Text($"Employee ID: {facultyEmployeeId}")
                                    .Bold().FontSize(8);
                            }
                        });

                        headerCol.Item().AlignCenter().Text($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}")
                            .FontSize(7);

                        headerCol.Item().PaddingVertical(3);
                    });

                    // === CONTENT - SCHEDULE GRID ===
                    page.Content().Table(table =>
                    {
                        // Calculate daily hours
                        var dailyHours = new Dictionary<DayOfWeek, double>();
                        foreach (var day in daysOfWeek)
                        {
                            dailyHours[day] = schedules
                                .Where(s => s.Day == day)
                                .Sum(s => (s.EndTime - s.StartTime).TotalHours);
                        }

                        // Define columns: Time + 6 days
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(65); // Time column
                            for (int i = 0; i < daysOfWeek.Length; i++)
                            {
                                columns.RelativeColumn(1); // Day columns
                            }
                        });

                        // === HEADER ROW ===
                        table.Header(header =>
                        {
                            // Time column header
                            header.Cell().Element(GridHeaderStyle).Text("TIME").Bold();

                            // Day column headers
                            foreach (var day in daysOfWeek)
                            {
                                header.Cell().Element(GridHeaderStyle).Text(day.ToString()).Bold();
                            }
                        });

                        // === TIME SLOT ROWS ===
                        foreach (var timeSlot in timeSlots)
                        {
                            // Time column
                            var timeDisplay = $"{FormatTime(timeSlot.Start)}-{FormatTime(timeSlot.End)}";
                            table.Cell().Element(TimeColumnStyle).Text(timeDisplay);

                            // Day columns
                            foreach (var day in daysOfWeek)
                            {
                                // Find schedules that overlap with this time slot on this day
                                var matchingSchedules = schedules.Where(s =>
                                    s.Day == day &&
                                    s.StartTime < timeSlot.End &&
                                    s.EndTime > timeSlot.Start
                                ).ToList();

                                if (matchingSchedules.Any())
                                {
                                    var schedule = matchingSchedules.First();

                                    // Calculate if this is the first slot for this schedule
                                    bool isFirstSlot = schedule.StartTime >= timeSlot.Start &&
                                                      schedule.StartTime < timeSlot.End;

                                    if (isFirstSlot)
                                    {
                                        // Calculate how many rows to span
                                        int rowsToSpan = CalculateSlotsToMerge(schedule, timeSlots, timeSlot);

                                        // Build cell content
                                        var subjectCode = schedule.Subject?.SubjectCode ?? "N/A";
                                        var subjectTitle = schedule.Subject?.SubjectTitle ?? "N/A";
                                        var courseCode = schedule.ClassSection?.CollegeCourse?.Code ?? "";
                                        var yearLevel = schedule.ClassSection?.YearLevel.ToString() ?? "";
                                        var section = schedule.ClassSection?.Section ?? "";
                                        var room = schedule.Room?.Name ?? "TBA";
                                        var timeRange = $"{FormatTime(schedule.StartTime)}-{FormatTime(schedule.EndTime)}";

                                        table.Cell().RowSpan((uint)rowsToSpan).Element(ScheduleCellStyle)
                                            .Column(col =>
                                            {
                                                col.Item().Text(timeRange).Bold().FontSize(6);
                                                col.Item().Text(subjectCode).Bold().FontSize(7);
                                                col.Item().Text(subjectTitle).FontSize(6);
                                                col.Item().Text($"{courseCode} {yearLevel}-{section}").FontSize(6);
                                                col.Item().Text($"Room: {room}").FontSize(6);
                                            });
                                    }
                                }
                                else
                                {
                                    // Empty cell
                                    table.Cell().Element(EmptyCellStyle);
                                }
                            }
                        }

                        // === DAILY HOURS SUMMARY ROW ===
                        table.Cell().Element(SummaryHeaderStyle).Text("Daily Hours").Bold();

                        foreach (var day in daysOfWeek)
                        {
                            var hours = dailyHours[day];
                            table.Cell().Element(SummaryCellStyle).Text($"{hours:F2} hrs").Bold();
                        }

                        // === WEEKLY SUMMARY SECTION ===
                        var totalWeeklyHours = dailyHours.Values.Sum();
                        var totalClasses = schedules.Count;
                        var uniqueSubjects = schedules.Select(s => s.SubjectId).Distinct().Count();

                        // Summary header
                        table.Cell().ColumnSpan((uint)(daysOfWeek.Length + 1))
                            .Element(WeeklySummaryHeaderStyle)
                            .Text("WEEKLY SUMMARY").Bold();

                        // Summary rows
                        var summaryItems = new[]
                        {
                            ("Total Teaching Hours per Week", $"{totalWeeklyHours:F2} hours"),
                            ("Total Classes", totalClasses.ToString()),
                            ("Unique Subjects", uniqueSubjects.ToString()),
                            ("Average Hours per Day", $"{(totalWeeklyHours / daysOfWeek.Length):F2} hours")
                        };

                        foreach (var (label, value) in summaryItems)
                        {
                            table.Cell().ColumnSpan(3).Element(SummaryLabelStyle).Text(label).Bold();
                            table.Cell().ColumnSpan((uint)(daysOfWeek.Length - 2))
                                .Element(SummaryValueStyle).AlignRight().Text(value).Bold();
                        }
                    });

                    // === FOOTER ===
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft()
                            .Text($"Total Classes: {schedules.Count}").FontSize(7);

                        row.RelativeItem().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });

                        row.RelativeItem().AlignRight()
                            .Text("PCNL Class Scheduling System").FontSize(7);
                    });
                });
            });

            return document.GeneratePdf();
        }

        #region Helper Methods

        private List<TimeSlot> GenerateTimeSlots(TimeSpan start, TimeSpan end, int intervalMinutes)
        {
            var slots = new List<TimeSlot>();
            var current = start;

            while (current < end)
            {
                var next = current.Add(TimeSpan.FromMinutes(intervalMinutes));
                slots.Add(new TimeSlot { Start = current, End = next });
                current = next;
            }

            return slots;
        }

        private int CalculateSlotsToMerge(Schedule schedule, List<TimeSlot> timeSlots, TimeSlot currentSlot)
        {
            int count = 0;
            int currentIndex = timeSlots.IndexOf(currentSlot);

            for (int i = currentIndex; i < timeSlots.Count; i++)
            {
                var slot = timeSlots[i];
                if (schedule.StartTime < slot.End && schedule.EndTime > slot.Start)
                {
                    count++;
                }
                else if (slot.Start >= schedule.EndTime)
                {
                    break;
                }
            }

            return count;
        }

        private string FormatTime(TimeSpan time)
        {
            var hours = time.Hours;
            var minutes = time.Minutes;
            var period = hours >= 12 ? "PM" : "AM";
            var displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;

            return $"{displayHours}:{minutes:D2} {period}";
        }

        #endregion

        #region Cell Styles

        private static IContainer GridHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Medium)
                .Border(1)
                .BorderColor(Colors.Grey.Darken1)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer TimeColumnStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer ScheduleCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Lighten4)
                .Border(1)
                .BorderColor(Colors.Grey.Darken1)
                .Padding(3)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer EmptyCellStyle(IContainer container)
        {
            return container
                .Background(Colors.White)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(3);
        }

        private static IContainer SummaryHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Orange.Medium)
                .Border(1)
                .BorderColor(Colors.Grey.Darken1)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer SummaryCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Orange.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Darken1)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer WeeklySummaryHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Medium)
                .Border(1)
                .BorderColor(Colors.Grey.Darken1)
                .Padding(4)
                .AlignCenter()
                .AlignMiddle();
        }

        private static IContainer SummaryLabelStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(4)
                .AlignLeft()
                .AlignMiddle();
        }

        private static IContainer SummaryValueStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten4)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(4)
                .AlignRight()
                .AlignMiddle();
        }

        #endregion

        private class TimeSlot
        {
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
        }
    }
}