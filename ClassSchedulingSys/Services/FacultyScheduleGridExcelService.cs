// ClassSchedulingSys/Services/FacultyScheduleGridExcelService.cs
using ClassSchedulingSys.Models;
using ClosedXML.Excel;

namespace ClassSchedulingSys.Services
{
    /// <summary>
    /// Service for generating faculty-specific schedule grid Excel files optimized for coupon printing
    /// </summary>
    public class FacultyScheduleGridExcelService
    {
        /// <summary>
        /// Generates a faculty schedule grid Excel with two worksheets:
        /// 1. Schedule Grid (time slots, days, accumulated hours) - optimized for printing
        /// 2. Daily Class Breakdown (detailed schedule information)
        /// </summary>
        public byte[] GenerateFacultyScheduleGrid(
            List<Schedule> schedules,
            string facultyName,
            string facultyEmployeeId,
            string semesterLabel,
            string schoolYearLabel)
        {
            using var workbook = new XLWorkbook();

            // WORKSHEET 1: Schedule Grid (optimized for coupon printing)
            CreateScheduleGridWorksheet(workbook, schedules, facultyName, facultyEmployeeId, semesterLabel, schoolYearLabel);

            // WORKSHEET 2: Daily Class Breakdown
            CreateDailyBreakdownWorksheet(workbook, schedules, facultyName, facultyEmployeeId, semesterLabel, schoolYearLabel);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #region Worksheet 1: Schedule Grid (Optimized for Printing)

        private void CreateScheduleGridWorksheet(
            XLWorkbook workbook,
            List<Schedule> schedules,
            string facultyName,
            string facultyEmployeeId,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add("Schedule Grid");

            // Generate time slots from 7:00 AM to 6:00 PM ONLY (removed excess after 6pm)
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

            int currentRow = 1;

            // === COMPACT HEADER SECTION (Reduced spacing) ===
            CreateCompactHeader(worksheet, facultyName, facultyEmployeeId, semesterLabel, schoolYearLabel, ref currentRow, daysOfWeek.Length);

            // === COLUMN HEADERS ===
            int headerRow = currentRow;

            // Time column
            worksheet.Cell(headerRow, 1).Value = "TIME";
            worksheet.Cell(headerRow, 1).Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                .Font.SetFontColor(XLColor.White)
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
            worksheet.Column(1).Width = 11; // ✅ Slightly reduced width

            // Day columns
            for (int i = 0; i < daysOfWeek.Length; i++)
            {
                int colIndex = i + 2;
                worksheet.Cell(headerRow, colIndex).Value = daysOfWeek[i].ToString();
                worksheet.Cell(headerRow, colIndex).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                    .Font.SetFontColor(XLColor.White)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
                worksheet.Column(colIndex).Width = 18; // ✅ Slightly reduced width
            }

            currentRow++;
            int gridStartRow = currentRow;

            // === TIME SLOT ROWS ===
            var dailyHours = new Dictionary<DayOfWeek, double>();
            foreach (var day in daysOfWeek)
            {
                dailyHours[day] = 0;
            }

            foreach (var timeSlot in timeSlots)
            {
                // IMPROVED: Single line time format (2:30 PM-3:00 PM instead of multi-line)
                var timeDisplay = $"{FormatTime(timeSlot.Start)}-{FormatTime(timeSlot.End)}";

                worksheet.Cell(currentRow, 1).Value = timeDisplay;
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetFontSize(8) // ✅ Reduced font size
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#F0F0F0"));

                // Day columns
                for (int i = 0; i < daysOfWeek.Length; i++)
                {
                    int colIndex = i + 2;
                    var day = daysOfWeek[i];

                    // Find schedules that overlap with this time slot on this day
                    var matchingSchedules = schedules.Where(s =>
                        s.Day == day &&
                        s.StartTime < timeSlot.End &&
                        s.EndTime > timeSlot.Start
                    ).ToList();

                    var cell = worksheet.Cell(currentRow, colIndex);

                    if (matchingSchedules.Any())
                    {
                        var schedule = matchingSchedules.First();

                        // Calculate if this is the first slot for this schedule
                        bool isFirstSlot = schedule.StartTime >= timeSlot.Start &&
                                          schedule.StartTime < timeSlot.End;

                        if (isFirstSlot)
                        {
                            // Calculate how many rows to merge
                            int slotsToMerge = CalculateSlotsToMerge(schedule, timeSlots, timeSlot);

                            if (slotsToMerge > 1)
                            {
                                worksheet.Range(currentRow, colIndex, currentRow + slotsToMerge - 1, colIndex).Merge();
                            }

                            // Calculate hours for this schedule
                            var scheduleHours = (schedule.EndTime - schedule.StartTime).TotalHours;
                            dailyHours[day] += scheduleHours;

                            // COMPACT: Build cell content with reduced text
                            var subjectCode = schedule.Subject?.SubjectCode ?? "N/A";
                            var subjectTitle = schedule.Subject?.SubjectTitle ?? "N/A";
                            var courseCode = schedule.ClassSection?.CollegeCourse?.Code ?? "";
                            var yearLevel = schedule.ClassSection?.YearLevel.ToString() ?? "";
                            var section = schedule.ClassSection?.Section ?? "";
                            var room = schedule.Room?.Name ?? "TBA";
                            var timeRange = $"{FormatTime(schedule.StartTime)}-{FormatTime(schedule.EndTime)}"; // ✅ Single line

                            // COMPACT: Reduced line breaks
                            var cellContent = $"{timeRange}\n{subjectCode}\n{subjectTitle}\n{courseCode} {yearLevel}-{section}\nRoom: {room}";

                            cell.Value = cellContent;

                            // Apply styling with subject color
                            var bgColor = GetColorFromHex(schedule.Subject?.Color ?? "#CCCCCC");
                            cell.Style
                                .Font.SetFontSize(7) // Reduced font size for compactness
                                .Font.SetBold(true)
                                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                                .Alignment.SetWrapText(true)
                                .Fill.SetBackgroundColor(bgColor)
                                .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                                .Border.SetInsideBorder(XLBorderStyleValues.Thin);
                        }
                    }
                    else
                    {
                        // Empty cell
                        cell.Style
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                            .Fill.SetBackgroundColor(XLColor.White);
                    }
                }

                currentRow++;
            }

            int gridEndRow = currentRow - 1;

            // === DAILY HOURS SUMMARY ROW ===
            currentRow++; // Reduced spacing
            worksheet.Cell(currentRow, 1).Value = "Daily Hours";
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(9) // Reduced font
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#FFC000"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

            for (int i = 0; i < daysOfWeek.Length; i++)
            {
                int colIndex = i + 2;
                var day = daysOfWeek[i];
                var hours = dailyHours[day];

                worksheet.Cell(currentRow, colIndex).Value = $"{hours:F2} hrs";
                worksheet.Cell(currentRow, colIndex).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(9) // Reduced font
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#FFC000"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
            }

            currentRow++; // Reduced spacing (was +=2)

            // === COMPACT WEEKLY SUMMARY ===
            var totalWeeklyHours = dailyHours.Values.Sum();
            var totalClasses = schedules.Count;
            var uniqueSubjects = schedules.Select(s => s.SubjectId).Distinct().Count();

            worksheet.Cell(currentRow, 1).Value = "WEEKLY SUMMARY";
            worksheet.Range(currentRow, 1, currentRow, daysOfWeek.Length + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(11) // Reduced font
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                .Font.SetFontColor(XLColor.White)
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
            currentRow++;

            var summaryItems = new[]
            {
                ("Total Teaching Hours per Week", $"{totalWeeklyHours:F2} hours"),
                ("Total Classes", totalClasses.ToString()),
                ("Unique Subjects", uniqueSubjects.ToString()),
                ("Average Hours per Day", $"{(totalWeeklyHours / daysOfWeek.Length):F2} hours")
            };

            foreach (var (label, value) in summaryItems)
            {
                worksheet.Cell(currentRow, 1).Value = label;
                worksheet.Cell(currentRow, 1).Style.Font.SetBold(true).Font.SetFontSize(9); // ✅ Reduced font
                worksheet.Range(currentRow, 1, currentRow, 2).Merge();

                worksheet.Cell(currentRow, 3).Value = value;
                worksheet.Cell(currentRow, 3).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(9) // Reduced font
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                worksheet.Range(currentRow, 3, currentRow, daysOfWeek.Length + 1).Merge();

                currentRow++;
            }

            // Freeze panes (header rows and time column)
            worksheet.SheetView.FreezeRows(headerRow);
            worksheet.SheetView.FreezeColumns(1);
        }

        private void CreateCompactHeader(
            IXLWorksheet worksheet,
            string facultyName,
            string facultyEmployeeId,
            string semesterLabel,
            string schoolYearLabel,
            ref int currentRow,
            int columnCount)
        {
            // COMPACT: Reduced font sizes and spacing

            // Title
            worksheet.Cell(currentRow, 1).Value = "PHILIPPINE COLLEGE OF NORTHWESTERN LUZON";
            worksheet.Range(currentRow, 1, currentRow, columnCount + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12) // Reduced from 14
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Subtitle
            worksheet.Cell(currentRow, 1).Value = "Faculty Teaching Schedule Grid";
            worksheet.Range(currentRow, 1, currentRow, columnCount + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(10) // Reduced from 12
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Faculty Info
            worksheet.Cell(currentRow, 1).Value = $"Faculty: {facultyName}";
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(9); // Reduced from 11

            if (!string.IsNullOrWhiteSpace(facultyEmployeeId))
            {
                worksheet.Cell(currentRow, 4).Value = $"Employee ID: {facultyEmployeeId}";
                worksheet.Range(currentRow, 4, currentRow, columnCount + 1).Merge();
                worksheet.Cell(currentRow, 4).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(9) // Reduced from 11
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }
            currentRow++;

            // Semester Info
            worksheet.Cell(currentRow, 1).Value = $"{semesterLabel} • SY {schoolYearLabel}";
            worksheet.Range(currentRow, 1, currentRow, columnCount + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetItalic(true)
                .Font.SetFontSize(9) // Reduced from 10
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Generated Date
            worksheet.Cell(currentRow, 1).Value = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}";
            worksheet.Range(currentRow, 1, currentRow, columnCount + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(8) // Reduced from 9
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++; // Only 1 space instead of 2
        }

        #endregion

        #region Worksheet 2: Daily Class Breakdown

        private void CreateDailyBreakdownWorksheet(
            XLWorkbook workbook,
            List<Schedule> schedules,
            string facultyName,
            string facultyEmployeeId,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add("Daily Class Breakdown");

            var daysOfWeek = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            int currentRow = 1;

            // === HEADER ===
            worksheet.Cell(currentRow, 1).Value = "PHILIPPINE COLLEGE OF NORTHWESTERN LUZON";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Daily Class Breakdown";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Faculty Info
            worksheet.Cell(currentRow, 1).Value = $"Faculty: {facultyName}";
            worksheet.Range(currentRow, 1, currentRow, 3).Merge();
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);

            if (!string.IsNullOrWhiteSpace(facultyEmployeeId))
            {
                worksheet.Cell(currentRow, 4).Value = $"Employee ID: {facultyEmployeeId}";
                worksheet.Range(currentRow, 4, currentRow, 7).Merge();
                worksheet.Cell(currentRow, 4).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"{semesterLabel} • SY {schoolYearLabel}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow += 2;

            // Set column widths
            worksheet.Column(1).Width = 15; // Time
            worksheet.Column(2).Width = 12; // Subject Code
            worksheet.Column(3).Width = 30; // Title
            worksheet.Column(4).Width = 15; // Section
            worksheet.Column(5).Width = 12; // Room
            worksheet.Column(6).Width = 8;  // Hours
            worksheet.Column(7).Width = 12; // Type

            // === DAILY BREAKDOWN ===
            foreach (var day in daysOfWeek)
            {
                var daySchedules = schedules
                    .Where(s => s.Day == day)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                if (!daySchedules.Any())
                    continue;

                // Day header
                worksheet.Cell(currentRow, 1).Value = day.ToString();
                worksheet.Range(currentRow, 1, currentRow, 7).Merge();
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetBold(true)
                    .Font.SetFontSize(11)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#FFC000"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                currentRow++;

                // Column headers
                var headers = new[] { "Time", "Subject", "Title", "Section", "Room", "Hours", "Type" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(currentRow, i + 1).Value = headers[i];
                    worksheet.Cell(currentRow, i + 1).Style
                        .Font.SetBold(true)
                        .Font.SetFontSize(9)
                        .Fill.SetBackgroundColor(XLColor.LightGray)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }
                currentRow++;

                // Schedule rows
                var dayTotalHours = 0.0;
                foreach (var schedule in daySchedules)
                {
                    var hours = (schedule.EndTime - schedule.StartTime).TotalHours;
                    dayTotalHours += hours;

                    // ✅ IMPROVED: Single line time format
                    worksheet.Cell(currentRow, 1).Value = $"{FormatTime(schedule.StartTime)}-{FormatTime(schedule.EndTime)}";
                    worksheet.Cell(currentRow, 2).Value = schedule.Subject?.SubjectCode ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = schedule.Subject?.SubjectTitle ?? "N/A";
                    worksheet.Cell(currentRow, 4).Value = $"{schedule.ClassSection?.CollegeCourse?.Code} {schedule.ClassSection?.YearLevel}-{schedule.ClassSection?.Section}";
                    worksheet.Cell(currentRow, 5).Value = schedule.Room?.Name ?? "TBA";
                    worksheet.Cell(currentRow, 6).Value = $"{hours:F2}";
                    worksheet.Cell(currentRow, 7).Value = schedule.Subject?.SubjectType ?? "N/A";

                    for (int col = 1; col <= 7; col++)
                    {
                        worksheet.Cell(currentRow, col).Style
                            .Font.SetFontSize(9)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    }
                    currentRow++;
                }

                // Day total
                worksheet.Cell(currentRow, 1).Value = "Day Total";
                worksheet.Range(currentRow, 1, currentRow, 5).Merge();
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#E7E6E6"));

                worksheet.Cell(currentRow, 6).Value = $"{dayTotalHours:F2}";
                worksheet.Cell(currentRow, 6).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#E7E6E6"));

                worksheet.Cell(currentRow, 7).Value = $"{daySchedules.Count} classes";
                worksheet.Cell(currentRow, 7).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#E7E6E6"));

                for (int col = 1; col <= 7; col++)
                {
                    worksheet.Cell(currentRow, col).Style
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                currentRow += 2;
            }
        }

        #endregion

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

        private XLColor GetColorFromHex(string hexColor)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
                    return XLColor.FromHtml("#CCCCCC");

                return XLColor.FromHtml(hexColor);
            }
            catch
            {
                return XLColor.FromHtml("#CCCCCC");
            }
        }

        #endregion

        private class TimeSlot
        {
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
        }
    }
}