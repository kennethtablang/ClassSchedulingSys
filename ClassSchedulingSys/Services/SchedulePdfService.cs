// ClassSchedulingSys/Services/SchedulePdfService.cs - FIXED WITH 12-HOUR FORMAT
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClassSchedulingSys.Services
{
    public class SchedulePdfService : ISchedulePdfService
    {
        /// <summary>
        /// Converts 24-hour time format to 12-hour format with AM/PM
        /// </summary>
        private string FormatTimeTo12Hour(TimeSpan time)
        {
            var hours = time.Hours;
            var minutes = time.Minutes;
            var period = hours >= 12 ? "PM" : "AM";

            // Convert to 12-hour format
            if (hours == 0)
                hours = 12; // Midnight
            else if (hours > 12)
                hours -= 12;

            return $"{hours}:{minutes:D2} {period}";
        }

        /// <summary>
        /// Formats a time range in 12-hour format
        /// </summary>
        private string FormatTimeRange(TimeSpan startTime, TimeSpan endTime)
        {
            return $"{FormatTimeTo12Hour(startTime)} - {FormatTimeTo12Hour(endTime)}";
        }

        public byte[] GenerateSchedulePdf(List<Schedule> schedules, string pov, string id)
        {
            var semesterName = schedules.FirstOrDefault()?.ClassSection?.Semester?.Name ?? "";
            var schoolYear = schedules.FirstOrDefault()?.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.Letter.Landscape());

                    // Header
                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text("Philippine College of Northwestern Luzon")
                                .FontSize(20).Bold().AlignCenter();

                            column.Item().Text($"Class Schedule — {pov}: {GetPovLabel(schedules, pov, id)}")
                                .FontSize(12).SemiBold().AlignCenter();

                            if (!string.IsNullOrWhiteSpace(semesterName) || !string.IsNullOrWhiteSpace(syLabel))
                            {
                                column.Item().Text($"{semesterName} SY {syLabel}")
                                    .FontSize(11).Italic().AlignCenter();
                            }

                            column.Item().PaddingVertical(5); // Add spacing
                        });

                    // Content table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f); // Subject (wider)
                            columns.RelativeColumn(1.5f); // Section
                            columns.RelativeColumn(2);    // Faculty
                            columns.RelativeColumn(1.5f); // Room
                            columns.RelativeColumn(1);    // Day
                            columns.RelativeColumn(2);    // Time (wider for 12-hour format)
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Subject").FontSize(11).SemiBold();
                            header.Cell().Element(CellStyle).Text("Section").FontSize(11).SemiBold();
                            header.Cell().Element(CellStyle).Text("Faculty").FontSize(11).SemiBold();
                            header.Cell().Element(CellStyle).Text("Room").FontSize(11).SemiBold();
                            header.Cell().Element(CellStyle).Text("Day").FontSize(11).SemiBold();
                            header.Cell().Element(CellStyle).Text("Time").FontSize(11).SemiBold();
                        });

                        // Data rows - sorted by day and time
                        foreach (var s in schedules.OrderBy(s => s.Day).ThenBy(s => s.StartTime))
                        {
                            table.Cell().Element(CellStyle).Text(s.Subject?.SubjectTitle ?? "N/A").FontSize(9);

                            table.Cell().Element(CellStyle)
                                .Text($"{s.ClassSection?.CollegeCourse?.Code} {s.ClassSection?.YearLevel}{s.ClassSection?.Section}")
                                .FontSize(9);

                            table.Cell().Element(CellStyle).Text(s.Faculty?.FullName ?? "N/A").FontSize(9);
                            table.Cell().Element(CellStyle).Text(s.Room?.Name ?? "N/A").FontSize(9);
                            table.Cell().Element(CellStyle).Text(s.Day.ToString()).FontSize(9);

                            // ✅ Use 12-hour format for time
                            table.Cell().Element(CellStyle)
                                .Text(FormatTimeRange(s.StartTime, s.EndTime))
                                .FontSize(9);
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateFacultyLoadReport(List<FacultySubjectAssignment> assignments, int? semesterId)
        {
            if (!assignments.Any())
                throw new ArgumentException("No assignments provided for report generation.");

            // Group assignments by faculty
            var groupedByFaculty = assignments
                .GroupBy(a => a.FacultyId)
                .ToList();

            // Get semester info for header
            var firstAssignment = assignments.First();
            var semesterInfo = firstAssignment.ClassSection?.Semester;
            var semesterLabel = semesterInfo != null
                ? $"{semesterInfo.Name} ({semesterInfo.SchoolYear.StartYear}-{semesterInfo.SchoolYear.EndYear})"
                : "Current Semester";

            var document = Document.Create(container =>
            {
                foreach (var facultyGroup in groupedByFaculty)
                {
                    var faculty = facultyGroup.First().Faculty;
                    var facultyAssignments = facultyGroup.ToList();

                    // Calculate totals
                    var totalUnits = facultyAssignments.Sum(a => a.Subject.Units);
                    var totalSubjects = facultyAssignments.Select(a => a.SubjectId).Distinct().Count();

                    // Add a page for each faculty member
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                // Institution Header
                                col.Item().AlignCenter().Text("Philippine College of Northwestern Luzon")
                                    .Bold().FontSize(14);
                                col.Item().AlignCenter().Text("Faculty Academic Load Report")
                                    .FontSize(12);
                                col.Item().AlignCenter().Text(semesterLabel)
                                    .FontSize(10);
                                col.Item().PaddingVertical(5);
                            });
                        });

                        page.Content().Column(column =>
                        {
                            // Faculty Information Section
                            column.Item().PaddingVertical(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(text =>
                                    {
                                        text.Span("Faculty Name: ").Bold();
                                        text.Span(faculty.FullName);
                                    });

                                    col.Item().Text(text =>
                                    {
                                        text.Span("Employee ID: ").Bold();
                                        text.Span(faculty.EmployeeID ?? "N/A");
                                    });

                                    col.Item().Text(text =>
                                    {
                                        text.Span("Email: ").Bold();
                                        text.Span(faculty.Email ?? "N/A");
                                    });
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().AlignRight().Text(text =>
                                    {
                                        text.Span("Total Units: ").Bold();
                                        text.Span(totalUnits.ToString()).FontColor(Colors.Blue.Medium);
                                    });

                                    col.Item().AlignRight().Text(text =>
                                    {
                                        text.Span("Total Subjects: ").Bold();
                                        text.Span(totalSubjects.ToString()).FontColor(Colors.Green.Medium);
                                    });

                                    col.Item().AlignRight().Text(text =>
                                    {
                                        text.Span("Report Generated: ").Bold();
                                        text.Span(DateTime.Now.ToString("MMM dd, yyyy"));
                                    });
                                });
                            });

                            // Assignments Table
                            column.Item().PaddingTop(15).Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);  // No.
                                    columns.RelativeColumn(2);   // Subject Code
                                    columns.RelativeColumn(3);   // Subject Title
                                    columns.RelativeColumn(1);   // Units
                                    columns.RelativeColumn(1);   // Type
                                    columns.RelativeColumn(2);   // Section
                                    columns.RelativeColumn(2);   // Course
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("No.").Bold();
                                    header.Cell().Element(CellStyle).Text("Subject Code").Bold();
                                    header.Cell().Element(CellStyle).Text("Subject Title").Bold();
                                    header.Cell().Element(CellStyle).Text("Units").Bold();
                                    header.Cell().Element(CellStyle).Text("Type").Bold();
                                    header.Cell().Element(CellStyle).Text("Section").Bold();
                                    header.Cell().Element(CellStyle).Text("Course").Bold();

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten1)
                                            .PaddingVertical(5)
                                            .Background(Colors.Grey.Lighten3);
                                    }
                                });

                                // Body rows
                                var counter = 1;
                                foreach (var assignment in facultyAssignments.OrderBy(a => a.Subject.SubjectCode))
                                {
                                    var isEvenRow = counter % 2 == 0;

                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).Text(counter.ToString());
                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).Text(assignment.Subject.SubjectCode);
                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).Text(assignment.Subject.SubjectTitle);
                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).AlignCenter().Text(assignment.Subject.Units.ToString());
                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).Text(assignment.Subject.SubjectType ?? "N/A");

                                    var sectionLabel = $"{assignment.ClassSection.YearLevel}-{assignment.ClassSection.Section}";
                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).Text(sectionLabel);

                                    table.Cell().Element(c => RowStyle(c, isEvenRow)).Text(assignment.ClassSection.CollegeCourse?.Code ?? "N/A");

                                    counter++;
                                }

                                static IContainer RowStyle(IContainer container, bool isEven)
                                {
                                    return container
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .Background(isEven ? Colors.Grey.Lighten4 : Colors.White)
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(5);
                                }
                            });

                            // Summary Section
                            column.Item().PaddingTop(20).Row(row =>
                            {
                                row.RelativeItem();

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().BorderTop(2).BorderColor(Colors.Grey.Medium).PaddingTop(5);

                                    col.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Total Teaching Load:").Bold();
                                        r.ConstantItem(80).AlignRight().Text($"{totalUnits} units").Bold().FontSize(11);
                                    });

                                    col.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.RelativeItem().Text("Number of Subjects:").Bold();
                                        r.ConstantItem(80).AlignRight().Text(totalSubjects.ToString()).Bold().FontSize(11);
                                    });
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                            text.Span(" | Generated by PCNL Class Scheduling System on ");
                            text.Span(DateTime.Now.ToString("MMMM dd, yyyy"));
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }

        private static IContainer CellStyle(IContainer container) =>
            container
                .PaddingVertical(5)
                .PaddingHorizontal(5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);

        private string GetPovLabel(List<Schedule> schedules, string pov, string id)
        {
            return pov.ToLower() switch
            {
                "faculty" => schedules.FirstOrDefault()?.Faculty?.FullName ?? id,
                "class section" or "classsection" => schedules.FirstOrDefault()?.ClassSection?.Section ?? id,
                "room" => schedules.FirstOrDefault()?.Room?.Name ?? id,
                _ => id
            };
        }
    }
}