// ClassSchedulingSys/Services/SchedulePdfService.cs - FIXED VERSION
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClassSchedulingSys.Services
{
    /// <summary>
    /// Service for generating PDF reports for schedules and faculty academic loads.
    /// Uses QuestPDF library for document generation.
    /// </summary>
    public class SchedulePdfService : ISchedulePdfService
    {
        private readonly IWebHostEnvironment _environment;

        public SchedulePdfService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Generates a PDF schedule report based on point of view (Faculty, Room, Class Section, or All)
        /// </summary>
        /// <param name="schedules">List of schedules to include in the report</param>
        /// <param name="pov">Point of view: Faculty, Room, ClassSection, or All</param>
        /// <param name="id">Identifier for the selected entity (faculty ID, room ID, etc.)</param>
        /// <returns>Byte array containing the generated PDF</returns>
        public byte[] GenerateSchedulePdf(List<Schedule> schedules, string pov, string id)
        {
            // ✅ FIX: Handle empty schedules list
            if (!schedules.Any())
            {
                throw new InvalidOperationException("No schedules found to generate PDF.");
            }

            // Extract semester information from first schedule
            var firstSchedule = schedules.FirstOrDefault();
            var semesterName = firstSchedule?.ClassSection?.Semester?.Name ?? "N/A";
            var schoolYear = firstSchedule?.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            // ✅ FIX: Determine if Faculty column should be hidden
            var isFacultyView = pov.Equals("Faculty", StringComparison.OrdinalIgnoreCase);

            // Get the logo path
            var logoPath = Path.Combine(_environment.ContentRootPath, "Assets", "PCNL_Logo.jpg");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Portrait());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // === HEADER ===
                    page.Header().Column(headerCol =>
                    {
                        // Centered row containing logo and title
                        headerCol.Item().AlignCenter().Row(row =>
                        {
                            // Logo on the left of the text
                            row.AutoItem().Column(col =>
                            {
                                if (File.Exists(logoPath))
                                {
                                    col.Item().AlignMiddle().Height(60).Width(60).Image(logoPath);
                                }
                            });

                            // Small space between logo and text
                            row.AutoItem().Width(15);

                            // Title section
                            row.AutoItem().Column(col =>
                            {
                                col.Item().AlignCenter().Text("Philippine College of Northwestern Luzon")
                                    .Bold().FontSize(16);

                                // ✅ FIX: Improved title generation
                                var titleLabel = GetPovLabel(schedules, pov, id);
                                col.Item().AlignCenter().Text($"Class Schedule — {titleLabel}")
                                    .FontSize(13).SemiBold();

                                if (!string.IsNullOrWhiteSpace(semesterName) || !string.IsNullOrWhiteSpace(syLabel))
                                {
                                    col.Item().AlignCenter().Text($"{semesterName} • SY {syLabel}")
                                        .FontSize(11).Italic();
                                }

                                col.Item().AlignCenter().Text($"Generated: {DateTime.Now:MMMM dd, yyyy}")
                                    .FontSize(9);
                            });
                        });
                    });

                    // === CONTENT TABLE ===
                    page.Content().PaddingTop(15).Table(table =>
                    {
                        // ✅ FIX: Dynamic column definition based on POV
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f); // Subject
                            columns.RelativeColumn(1.5f); // Section
                            if (!isFacultyView)
                            {
                                columns.RelativeColumn(2f);   // Faculty (only if not Faculty view)
                            }
                            columns.RelativeColumn(1.2f); // Room
                            columns.RelativeColumn(1f);   // Day
                            columns.RelativeColumn(1.5f); // Time
                            columns.RelativeColumn(0.8f); // Units
                        });

                        //  FIX: Dynamic header row based on POV
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Day").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Time").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Subject").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Room").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Section").Bold();
                            if (!isFacultyView)
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Faculty").Bold();
                            }
                            header.Cell().Element(HeaderCellStyle).Text("Units").Bold();
                        });

                        // Data rows - sorted by day then time
                        var orderedSchedules = schedules
                            .OrderBy(s => s.Day)
                            .ThenBy(s => s.StartTime)
                            .ToList();

                        var rowCounter = 0;
                        foreach (var s in orderedSchedules)
                        {
                            var isEvenRow = rowCounter % 2 == 0;

                            // Day
                            table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(FormatDay(s.Day));

                            // Time formatted in 12-hour format
                            var timeDisplay = $"{FormatTime(s.StartTime)}\n{FormatTime(s.EndTime)}";
                            table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(timeDisplay).FontSize(9);

                            // Subject with code
                            var subjectDisplay = $"{s.Subject?.SubjectCode ?? "N/A"}\n{s.Subject?.SubjectTitle ?? "N/A"}";
                            table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(subjectDisplay).FontSize(9);

                            // Section with course code
                            var sectionDisplay = $"{s.ClassSection?.CollegeCourse?.Code ?? ""} {s.ClassSection?.YearLevel}{s.ClassSection?.Section ?? ""}";
                            table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(sectionDisplay.Trim());

                            // Room
                            table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(s.Room?.Name ?? "N/A");

                            // Faculty
                            if (!isFacultyView)
                            {
                                table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(s.Faculty?.FullName ?? "N/A");
                            }

                            // Units
                            table.Cell().Element(c => DataCellStyle(c, isEvenRow)).AlignCenter().Text(s.Subject?.Units.ToString() ?? "0");

                            rowCounter++;
                        }
                    });

                    // === FOOTER ===
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text(text =>
                        {
                            text.Span("Total Schedules: ").Bold();
                            text.Span(schedules.Count.ToString());
                        });

                        row.RelativeItem().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });

                        row.RelativeItem().AlignRight().Text("PCNL Class Scheduling System").FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates a PDF schedule for a specific course, year level, and block
        /// </summary>
        public byte[] GenerateCourseBlockSchedulePdf(
            List<Schedule> schedules,
            string courseCode,
            string courseName,
            int yearLevel,
            string blockLabel,
            string semesterLabel,
            string schoolYearLabel)
        {
            var logoPath = Path.Combine(_environment.ContentRootPath, "Assets", "PCNL_Logo.jpg");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter.Portrait());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().AlignCenter().Row(row =>
                        {
                            row.AutoItem().Column(col =>
                            {
                                if (File.Exists(logoPath))
                                {
                                    col.Item().AlignMiddle().Height(50).Width(50).Image(logoPath);
                                }
                            });

                            row.AutoItem().Width(15);

                            row.AutoItem().Column(col =>
                            {
                                col.Item().AlignCenter().Text("PHILIPPINE COLLEGE OF NORTHWESTERN LUZON")
                                    .Bold().FontSize(14);
                                col.Item().AlignCenter().Text("San Antonio, Agoo, La Union")
                                    .FontSize(10);
                                col.Item().AlignCenter().Text($"{courseName}")
                                    .Bold().FontSize(12);
                            });
                        });

                        headerCol.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Revised Curriculum (2023-2024)")
                                    .FontSize(9);
                                col.Item().Text($"SY {schoolYearLabel}")
                                    .FontSize(9);
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().AlignRight().Text($"{semesterLabel}")
                                    .Bold().FontSize(10);
                                col.Item().AlignRight().Text($"Year-Block: {yearLevel} - {blockLabel}")
                                    .Bold().FontSize(10);
                            });
                        });
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(3f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("TIME").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("DAY").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("COURSE CODE").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("DESCRIPTIVE TITLE").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("UNITS").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("ROOM").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("INSTRUCTOR").Bold();
                        });

                        var orderedSchedules = schedules
                            .OrderBy(s => s.Day)
                            .ThenBy(s => s.StartTime)
                            .ToList();

                        var sectionGroups = orderedSchedules
                            .GroupBy(s => s.ClassSection.Section)
                            .OrderBy(g => g.Key);

                        foreach (var sectionGroup in sectionGroups)
                        {
                            if (sectionGroups.Count() > 1)
                            {
                                table.Cell().ColumnSpan(7).Element(SectionHeaderStyle)
                                    .Text($"BLOCK {sectionGroup.Key}").Bold();
                            }

                            var rowCounter = 0;
                            foreach (var s in sectionGroup)
                            {
                                var isEvenRow = rowCounter % 2 == 0;

                                var timeDisplay = $"{FormatTime(s.StartTime)} - {FormatTime(s.EndTime)}";
                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .Text(timeDisplay).FontSize(8);

                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .Text(FormatDay(s.Day));

                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .Text(s.Subject?.SubjectCode ?? "N/A");

                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .Text(s.Subject?.SubjectTitle ?? "N/A").FontSize(8);

                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .AlignCenter().Text(s.Subject?.Units.ToString() ?? "0");

                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .Text(s.Room?.Name ?? "TBA");

                                table.Cell().Element(c => DataCellStyle(c, isEvenRow))
                                    .Text(s.Faculty?.FullName ?? "TBA").FontSize(8);

                                rowCounter++;
                            }

                            if (sectionGroup.Any())
                            {
                                var totalUnits = sectionGroup.Sum(s => s.Subject?.Units ?? 0);

                                table.Cell().ColumnSpan(4).Element(TotalCellStyle)
                                    .AlignRight().Text("Total").Bold();

                                table.Cell().Element(TotalCellStyle)
                                    .AlignCenter().Text(totalUnits.ToString()).Bold();

                                table.Cell().ColumnSpan(2).Element(TotalCellStyle);
                            }
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text(text =>
                        {
                            text.Span("Total Subjects: ").Bold();
                            text.Span(schedules.Select(s => s.SubjectId).Distinct().Count().ToString());
                        });

                        row.RelativeItem().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });

                        row.RelativeItem().AlignRight().Text($"Generated: {DateTime.Now:MMM dd, yyyy}").FontSize(7);
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates a PDF report of faculty academic loads
        /// </summary>
        public byte[] GenerateFacultyLoadReport(List<FacultySubjectAssignment> assignments, int? semesterId)
        {
            if (!assignments.Any())
                throw new ArgumentException("No assignments provided for report generation.");

            var groupedByFaculty = assignments
                .GroupBy(a => a.FacultyId)
                .ToList();

            var firstAssignment = assignments.First();
            var semesterInfo = firstAssignment.ClassSection?.Semester;
            var semesterLabel = semesterInfo != null
                ? $"{semesterInfo.Name} ({semesterInfo.SchoolYear.StartYear}-{semesterInfo.SchoolYear.EndYear})"
                : "Current Semester";

            var logoPath = Path.Combine(_environment.ContentRootPath, "Assets", "PCNL_Logo.jpg");

            var document = Document.Create(container =>
            {
                foreach (var facultyGroup in groupedByFaculty)
                {
                    var faculty = facultyGroup.First().Faculty;
                    var facultyAssignments = facultyGroup.ToList();

                    var totalUnits = facultyAssignments.Sum(a => a.Subject.Units);
                    var totalSubjects = facultyAssignments.Select(a => a.SubjectId).Distinct().Count();

                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter.Portrait());
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().AlignCenter().Row(row =>
                            {
                                row.AutoItem().Column(col =>
                                {
                                    if (File.Exists(logoPath))
                                    {
                                        col.Item().AlignMiddle().Height(60).Width(60).Image(logoPath);
                                    }
                                });

                                row.AutoItem().Width(15);

                                row.AutoItem().Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Philippine College of Northwestern Luzon")
                                        .Bold().FontSize(16);

                                    col.Item().AlignCenter().Text("San Antonio, Agoo, La Union 2504 Philippines")
                                        .FontSize(12);

                                    col.Item().AlignCenter().Text("Telephone No. (072) 607-3883")
                                        .FontSize(12);
                                });
                            });
                        });

                        page.Content().Column(column =>
                        {
                            column.Item().PaddingVertical(6);

                            column.Item().AlignCenter().Text("CONSOLIDATED FACULTY LOADING")
                                .FontSize(16).SemiBold();

                            column.Item().AlignCenter().Text(semesterLabel)
                                .FontSize(11).Italic();

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
                                        text.Span(totalUnits.ToString()).FontColor(Colors.Blue.Medium).FontSize(12);
                                    });

                                    col.Item().AlignRight().Text(text =>
                                    {
                                        text.Span("Total Subjects: ").Bold();
                                        text.Span(totalSubjects.ToString()).FontColor(Colors.Green.Medium).FontSize(12);
                                    });

                                    col.Item().AlignRight().Text(text =>
                                    {
                                        text.Span("Report Generated: ").Bold();
                                        text.Span(DateTime.Now.ToString("MMM dd, yyyy"));
                                    });
                                });
                            });

                            column.Item().PaddingTop(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(35);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(0.8f);
                                    columns.RelativeColumn(1f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1f);
                                    columns.RelativeColumn(1.3f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("No.").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Subject Code").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Subject Title").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Units").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Type").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Section").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Course").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Day").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Time").Bold();
                                });

                                var counter = 1;
                                var orderedAssignments = facultyAssignments
                                    .OrderBy(a => a.Subject.SubjectCode)
                                    .ThenBy(a => a.ClassSection.Section)
                                    .ToList();

                                foreach (var assignment in orderedAssignments)
                                {
                                    var isEvenRow = counter % 2 == 0;

                                    var schedule = assignment.Subject.Schedules?
                                        .FirstOrDefault(s =>
                                            s.SubjectId == assignment.SubjectId &&
                                            s.ClassSectionId == assignment.ClassSectionId &&
                                            s.FacultyId == faculty.Id);

                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).AlignCenter().Text(counter.ToString());
                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(assignment.Subject.SubjectCode);
                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(assignment.Subject.SubjectTitle);
                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).AlignCenter().Text(assignment.Subject.Units.ToString());
                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(assignment.Subject.SubjectType ?? "N/A");

                                    var sectionLabel = $"{assignment.ClassSection.YearLevel}-{assignment.ClassSection.Section}";
                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(sectionLabel);
                                    table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(assignment.ClassSection.CollegeCourse?.Code ?? "N/A");

                                    if (schedule != null)
                                    {
                                        table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(FormatDay(schedule.Day));

                                        var timeDisplay = $"{FormatTime(schedule.StartTime)} - {FormatTime(schedule.EndTime)}";
                                        table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text(timeDisplay).FontSize(9);
                                    }
                                    else
                                    {
                                        table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text("TBA").Italic().FontColor(Colors.Grey.Medium);
                                        table.Cell().Element(c => DataCellStyle(c, isEvenRow)).Text("TBA").Italic().FontColor(Colors.Grey.Medium);
                                    }

                                    counter++;
                                }
                            });

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

                                    var scheduledCount = facultyAssignments.Count(a =>
                                        a.Subject.Schedules?.Any(s =>
                                            s.SubjectId == a.SubjectId &&
                                            s.ClassSectionId == a.ClassSectionId &&
                                            s.FacultyId == faculty.Id) == true);

                                    var unscheduledCount = facultyAssignments.Count - scheduledCount;

                                    col.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.RelativeItem().Text("Scheduled Subjects:").Bold();
                                        r.ConstantItem(80).AlignRight().Text($"{scheduledCount} / {totalSubjects}").Bold().FontSize(11);
                                    });

                                    if (unscheduledCount > 0)
                                    {
                                        col.Item().PaddingTop(5).Row(r =>
                                        {
                                            r.RelativeItem().Text("⚠ Unscheduled Subjects:").Bold().FontColor(Colors.Orange.Medium);
                                            r.ConstantItem(80).AlignRight().Text(unscheduledCount.ToString()).Bold().FontSize(11).FontColor(Colors.Orange.Medium);
                                        });
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                            text.Span(" | Generated by PCNL Class Scheduling System");
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }

        #region Helper Methods

        private static string FormatTime(TimeSpan time)
        {
            var hours = time.Hours;
            var minutes = time.Minutes;
            var period = hours >= 12 ? "PM" : "AM";
            var displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;

            return $"{displayHours}:{minutes:D2} {period}";
        }

        private string FormatDay(object dayValue)
        {
            if (dayValue == null) return "TBA";
            string dayName = dayValue.ToString();
            if (dayName.Length >= 3)
                return dayName.Substring(0, 3);
            return dayName;
        }

        /// <summary>
        /// Gets a readable label for the selected entity based on point of view
        /// </summary>
        private static string GetPovLabel(List<Schedule> schedules, string pov, string id)
        {
            // ✅ FIX: Handle "All" POV case
            if (string.IsNullOrWhiteSpace(pov) || pov.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                return "All Schedules";
            }

            // ✅ FIX: Handle empty schedules list
            if (!schedules.Any())
            {
                return $"{pov}: {id ?? "N/A"}";
            }

            // ✅ FIX: Improved entity name retrieval with null checks
            return pov.ToLower() switch
            {
                "faculty" => schedules.FirstOrDefault()?.Faculty?.FullName ?? $"Faculty: {id ?? "Unknown"}",
                "class section" or "classsection" =>
                    GetClassSectionLabel(schedules.FirstOrDefault()?.ClassSection) ?? $"Section: {id ?? "Unknown"}",
                "room" => schedules.FirstOrDefault()?.Room?.Name ?? $"Room: {id ?? "Unknown"}",
                _ => $"{pov}: {id ?? "Unknown"}"
            };
        }

        /// <summary>
        /// Helper method to format class section label
        /// </summary>
        private static string GetClassSectionLabel(ClassSection? classSection)
        {
            if (classSection == null) return null;

            var courseCode = classSection.CollegeCourse?.Code ?? "";
            var yearLevel = classSection.YearLevel;
            var section = classSection.Section ?? "";

            return $"{courseCode} {yearLevel}-{section}".Trim();
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken1)
                .Background(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(5);
        }

        private static IContainer DataCellStyle(IContainer container, bool isEvenRow)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(isEvenRow ? Colors.Grey.Lighten4 : Colors.White)
                .PaddingVertical(4)
                .PaddingHorizontal(5);
        }

        private static IContainer SectionHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Lighten3)
                .PaddingVertical(4)
                .PaddingHorizontal(5);
        }

        private static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .BorderTop(1)
                .BorderColor(Colors.Grey.Darken1)
                .Background(Colors.Grey.Lighten3)
                .PaddingVertical(4)
                .PaddingHorizontal(5);
        }

        #endregion
    }
}