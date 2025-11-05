using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ClassSchedulingSys.Services
{
    public class ScheduleExcelService : IScheduleExcelService
    {
        private readonly IWebHostEnvironment _environment;

        public ScheduleExcelService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Generates an Excel file with separate worksheets for each day of the week
        /// </summary>
        public byte[] GenerateScheduleExcel(
            List<Schedule> schedules,
            string pov,
            string id,
            string semesterLabel,
            string schoolYearLabel)
        {
            using var workbook = new XLWorkbook();

            // Get entity name for title
            var entityName = GetEntityName(schedules, pov, id);

            // Days of the week in order
            var daysOfWeek = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };

            // Create a worksheet for each day
            foreach (var day in daysOfWeek)
            {
                var daySchedules = schedules
                    .Where(s => s.Day == day)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                CreateDayWorksheet(
                    workbook,
                    day.ToString(),
                    daySchedules,
                    entityName,
                    pov,
                    semesterLabel,
                    schoolYearLabel);
            }

            // Create summary worksheet
            CreateSummaryWorksheet(
                workbook,
                schedules,
                entityName,
                pov,
                semesterLabel,
                schoolYearLabel);

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Generates a room utilization report with detailed usage statistics
        /// </summary>
        public byte[] GenerateRoomUtilizationExcel(
            List<Schedule> schedules,
            List<Room> allRooms,
            string semesterLabel,
            string schoolYearLabel)
        {
            using var workbook = new XLWorkbook();

            // Create overview sheet
            CreateRoomUtilizationOverview(
                workbook,
                schedules,
                allRooms,
                semesterLabel,
                schoolYearLabel);

            // Create daily breakdown sheets
            var daysOfWeek = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            foreach (var day in daysOfWeek)
            {
                CreateRoomUtilizationByDay(
                    workbook,
                    schedules.Where(s => s.Day == day).ToList(),
                    allRooms,
                    day.ToString(),
                    semesterLabel,
                    schoolYearLabel);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #region Day Worksheet Creation

        private void CreateDayWorksheet(
            XLWorkbook workbook,
            string dayName,
            List<Schedule> daySchedules,
            string entityName,
            string pov,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add(dayName);

            // Set column widths for better readability
            worksheet.Column(1).Width = 12;  // Time
            worksheet.Column(2).Width = 15;  // Room
            worksheet.Column(3).Width = 12;  // Subject Code
            worksheet.Column(4).Width = 30;  // Subject Title
            worksheet.Column(5).Width = 25;  // Faculty
            worksheet.Column(6).Width = 12;  // Section
            worksheet.Column(7).Width = 15;  // Course
            worksheet.Column(8).Width = 8;   // Units
            worksheet.Column(9).Width = 15;  // Building

            int currentRow = 1;

            // === HEADER SECTION ===
            // Title
            worksheet.Cell(currentRow, 1).Value = "PHILIPPINE COLLEGE OF NORTHWESTERN LUZON";
            worksheet.Range(currentRow, 1, currentRow, 9).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Subtitle
            worksheet.Cell(currentRow, 1).Value = $"Class Schedule - {pov}: {entityName}";
            worksheet.Range(currentRow, 1, currentRow, 9).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Semester info
            worksheet.Cell(currentRow, 1).Value = $"{semesterLabel} • SY {schoolYearLabel}";
            worksheet.Range(currentRow, 1, currentRow, 9).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetItalic(true)
                .Font.SetFontSize(10)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            // Day label
            worksheet.Cell(currentRow, 1).Value = $"Day: {dayName}";
            worksheet.Range(currentRow, 1, currentRow, 9).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(11)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.LightGray);
            currentRow += 2;

            // === TABLE HEADERS ===
            var headers = new[] { "Time", "Room", "Subject Code", "Subject Title", "Faculty", "Section", "Course", "Units", "Building" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            currentRow++;

            // === DATA ROWS ===
            if (!daySchedules.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "No classes scheduled for this day";
                worksheet.Range(currentRow, 1, currentRow, 9).Merge();
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.LightGray);
            }
            else
            {
                foreach (var schedule in daySchedules)
                {
                    var isEvenRow = (currentRow - 6) % 2 == 0;
                    var bgColor = isEvenRow ? XLColor.FromHtml("#E7E6E6") : XLColor.White;

                    // Time
                    worksheet.Cell(currentRow, 1).Value = $"{FormatTime(schedule.StartTime)} - {FormatTime(schedule.EndTime)}";
                    worksheet.Cell(currentRow, 1).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Room
                    worksheet.Cell(currentRow, 2).Value = schedule.Room?.Name ?? "TBA";
                    worksheet.Cell(currentRow, 2).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Subject Code
                    worksheet.Cell(currentRow, 3).Value = schedule.Subject?.SubjectCode ?? "N/A";
                    worksheet.Cell(currentRow, 3).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Subject Title
                    worksheet.Cell(currentRow, 4).Value = schedule.Subject?.SubjectTitle ?? "N/A";
                    worksheet.Cell(currentRow, 4).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Faculty
                    worksheet.Cell(currentRow, 5).Value = schedule.Faculty?.FullName ?? "N/A";
                    worksheet.Cell(currentRow, 5).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Section
                    var sectionLabel = $"{schedule.ClassSection?.YearLevel}{schedule.ClassSection?.Section ?? ""}";
                    worksheet.Cell(currentRow, 6).Value = sectionLabel;
                    worksheet.Cell(currentRow, 6).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Course
                    worksheet.Cell(currentRow, 7).Value = schedule.ClassSection?.CollegeCourse?.Code ?? "N/A";
                    worksheet.Cell(currentRow, 7).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Units
                    worksheet.Cell(currentRow, 8).Value = schedule.Subject?.Units ?? 0;
                    worksheet.Cell(currentRow, 8).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    // Building
                    worksheet.Cell(currentRow, 9).Value = schedule.Room?.Building?.Name ?? "N/A";
                    worksheet.Cell(currentRow, 9).Style
                        .Fill.SetBackgroundColor(bgColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    currentRow++;
                }
            }

            // === FOOTER ===
            currentRow += 2;
            worksheet.Cell(currentRow, 1).Value = $"Total Classes: {daySchedules.Count}";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);

            worksheet.Cell(currentRow, 9).Value = $"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}";
            worksheet.Cell(currentRow, 9).Style
                .Font.SetFontSize(8)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            // Freeze header rows
            worksheet.SheetView.FreezeRows(6);
        }

        #endregion

        #region Summary Worksheet

        private void CreateSummaryWorksheet(
            XLWorkbook workbook,
            List<Schedule> schedules,
            string entityName,
            string pov,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add("Summary");

            // Set column widths
            worksheet.Column(1).Width = 20;
            worksheet.Column(2).Width = 15;
            worksheet.Column(3).Width = 20;
            worksheet.Column(4).Width = 15;

            int currentRow = 1;

            // === TITLE ===
            worksheet.Cell(currentRow, 1).Value = "SCHEDULE SUMMARY REPORT";
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow += 2;

            // === GENERAL INFO ===
            worksheet.Cell(currentRow, 1).Value = "Entity:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = $"{pov} - {entityName}";
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Semester:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = semesterLabel;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "School Year:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = schoolYearLabel;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Generated:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
            currentRow += 2;

            // === STATISTICS ===
            worksheet.Cell(currentRow, 1).Value = "STATISTICS";
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                .Font.SetFontColor(XLColor.White);
            currentRow++;

            var totalClasses = schedules.Count;
            var totalHours = schedules.Sum(s => (s.EndTime - s.StartTime).TotalHours);
            var uniqueSubjects = schedules.Select(s => s.SubjectId).Distinct().Count();
            var uniqueFaculty = schedules.Select(s => s.FacultyId).Distinct().Count();
            var uniqueRooms = schedules.Select(s => s.RoomId).Distinct().Count();

            var stats = new[]
            {
                ("Total Classes", totalClasses.ToString()),
                ("Total Hours", $"{totalHours:F2} hours"),
                ("Unique Subjects", uniqueSubjects.ToString()),
                ("Faculty Members", uniqueFaculty.ToString()),
                ("Rooms Used", uniqueRooms.ToString())
            };

            foreach (var (label, value) in stats)
            {
                worksheet.Cell(currentRow, 1).Value = label;
                worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
                worksheet.Cell(currentRow, 2).Value = value;
                worksheet.Cell(currentRow, 2).Style
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                    .Font.SetBold(true);
                currentRow++;
            }

            currentRow += 2;

            // === BREAKDOWN BY DAY ===
            worksheet.Cell(currentRow, 1).Value = "BREAKDOWN BY DAY";
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                .Font.SetFontColor(XLColor.White);
            currentRow++;

            // Table headers
            worksheet.Cell(currentRow, 1).Value = "Day";
            worksheet.Cell(currentRow, 2).Value = "Classes";
            worksheet.Cell(currentRow, 3).Value = "Hours";
            worksheet.Cell(currentRow, 4).Value = "Percentage";

            for (int col = 1; col <= 4; col++)
            {
                worksheet.Cell(currentRow, col).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            currentRow++;

            var daysOfWeek = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };

            foreach (var day in daysOfWeek)
            {
                var daySchedules = schedules.Where(s => s.Day == day).ToList();
                var dayClasses = daySchedules.Count;
                var dayHours = daySchedules.Sum(s => (s.EndTime - s.StartTime).TotalHours);
                var percentage = totalClasses > 0 ? (dayClasses * 100.0 / totalClasses) : 0;

                worksheet.Cell(currentRow, 1).Value = day.ToString();
                worksheet.Cell(currentRow, 2).Value = dayClasses;
                worksheet.Cell(currentRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                worksheet.Cell(currentRow, 3).Value = $"{dayHours:F2}";
                worksheet.Cell(currentRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                worksheet.Cell(currentRow, 4).Value = $"{percentage:F1}%";
                worksheet.Cell(currentRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // Alternating row colors
                if ((currentRow % 2) == 0)
                {
                    for (int col = 1; col <= 4; col++)
                    {
                        worksheet.Cell(currentRow, col).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E7E6E6"));
                    }
                }

                for (int col = 1; col <= 4; col++)
                {
                    worksheet.Cell(currentRow, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                currentRow++;
            }
        }

        #endregion

        #region Room Utilization

        private void CreateRoomUtilizationOverview(
            XLWorkbook workbook,
            List<Schedule> schedules,
            List<Room> allRooms,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add("Overview");

            worksheet.Column(1).Width = 15;  // Room
            worksheet.Column(2).Width = 15;  // Building
            worksheet.Column(3).Width = 12;  // Capacity
            worksheet.Column(4).Width = 12;  // Total Hours
            worksheet.Column(5).Width = 15;  // Utilization %
            worksheet.Column(6).Width = 12;  // Classes
            worksheet.Column(7).Width = 15;  // Status

            int currentRow = 1;

            // === HEADER ===
            worksheet.Cell(currentRow, 1).Value = "ROOM UTILIZATION REPORT";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"{semesterLabel} • SY {schoolYearLabel}";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow += 2;

            // === TABLE HEADERS ===
            var headers = new[] { "Room", "Building", "Capacity", "Total Hours", "Utilization %", "Classes", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            currentRow++;

            // Calculate room statistics
            var roomStats = allRooms.Select(room =>
            {
                var roomSchedules = schedules.Where(s => s.RoomId == room.Id).ToList();
                var totalHours = roomSchedules.Sum(s => (s.EndTime - s.StartTime).TotalHours);
                var classCount = roomSchedules.Count;

                // Assuming 40 hours per week available (8 hours/day * 5 days)
                var availableHours = 40.0;
                var utilization = availableHours > 0 ? (totalHours / availableHours * 100) : 0;

                return new
                {
                    Room = room,
                    TotalHours = totalHours,
                    ClassCount = classCount,
                    Utilization = utilization
                };
            }).OrderByDescending(r => r.Utilization).ToList();

            // === DATA ROWS ===
            foreach (var stat in roomStats)
            {
                var isEvenRow = (currentRow - 5) % 2 == 0;
                var bgColor = isEvenRow ? XLColor.FromHtml("#E7E6E6") : XLColor.White;

                // Status determination
                string status;
                XLColor statusColor;
                if (stat.Utilization >= 80)
                {
                    status = "High Usage";
                    statusColor = XLColor.FromHtml("#92D050");
                }
                else if (stat.Utilization >= 50)
                {
                    status = "Moderate";
                    statusColor = XLColor.FromHtml("#FFC000");
                }
                else if (stat.Utilization > 0)
                {
                    status = "Low Usage";
                    statusColor = XLColor.FromHtml("#FF6B6B");
                }
                else
                {
                    status = "Unused";
                    statusColor = XLColor.FromHtml("#CCCCCC");
                }

                // Room
                worksheet.Cell(currentRow, 1).Value = stat.Room.Name;
                worksheet.Cell(currentRow, 1).Style
                    .Fill.SetBackgroundColor(bgColor)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Building
                worksheet.Cell(currentRow, 2).Value = stat.Room.Building?.Name ?? "N/A";
                worksheet.Cell(currentRow, 2).Style
                    .Fill.SetBackgroundColor(bgColor)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Capacity
                worksheet.Cell(currentRow, 3).Value = stat.Room.Capacity;
                worksheet.Cell(currentRow, 3).Style
                    .Fill.SetBackgroundColor(bgColor)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Total Hours
                worksheet.Cell(currentRow, 4).Value = $"{stat.TotalHours:F2}";
                worksheet.Cell(currentRow, 4).Style
                    .Fill.SetBackgroundColor(bgColor)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Utilization %
                worksheet.Cell(currentRow, 5).Value = $"{stat.Utilization:F1}%";
                worksheet.Cell(currentRow, 5).Style
                    .Fill.SetBackgroundColor(bgColor)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Classes
                worksheet.Cell(currentRow, 6).Value = stat.ClassCount;
                worksheet.Cell(currentRow, 6).Style
                    .Fill.SetBackgroundColor(bgColor)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Status
                worksheet.Cell(currentRow, 7).Value = status;
                worksheet.Cell(currentRow, 7).Style
                    .Fill.SetBackgroundColor(statusColor)
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                currentRow++;
            }

            // === SUMMARY STATISTICS ===
            currentRow += 2;
            worksheet.Cell(currentRow, 1).Value = "SUMMARY";
            worksheet.Range(currentRow, 1, currentRow, 7).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Fill.SetBackgroundColor(XLColor.LightGray);
            currentRow++;

            var totalRooms = allRooms.Count;
            var usedRooms = roomStats.Count(r => r.ClassCount > 0);
            var avgUtilization = roomStats.Average(r => r.Utilization);
            var highUsageRooms = roomStats.Count(r => r.Utilization >= 80);
            var lowUsageRooms = roomStats.Count(r => r.Utilization > 0 && r.Utilization < 50);

            worksheet.Cell(currentRow, 1).Value = "Total Rooms:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = totalRooms;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Rooms in Use:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = usedRooms;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Average Utilization:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = $"{avgUtilization:F1}%";
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "High Usage Rooms:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = highUsageRooms;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Low Usage Rooms:";
            worksheet.Cell(currentRow, 1).Style.Font.SetBold(true);
            worksheet.Cell(currentRow, 2).Value = lowUsageRooms;

            // Freeze header rows
            worksheet.SheetView.FreezeRows(5);
        }

        private void CreateRoomUtilizationByDay(
            XLWorkbook workbook,
            List<Schedule> daySchedules,
            List<Room> allRooms,
            string dayName,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add($"{dayName} Usage");

            worksheet.Column(1).Width = 15;  // Room
            worksheet.Column(2).Width = 12;  // Time Slot
            worksheet.Column(3).Width = 12;  // Subject Code
            worksheet.Column(4).Width = 25;  // Subject Title
            worksheet.Column(5).Width = 20;  // Faculty
            worksheet.Column(6).Width = 12;  // Section

            int currentRow = 1;

            // === HEADER ===
            worksheet.Cell(currentRow, 1).Value = $"ROOM USAGE - {dayName.ToUpper()}";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"{semesterLabel} • SY {schoolYearLabel}";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetItalic(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow += 2;

            // === TABLE HEADERS ===
            var headers = new[] { "Room", "Time Slot", "Subject Code", "Subject Title", "Faculty", "Section" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(currentRow, i + 1);
                cell.Value = headers[i];
                cell.Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#4472C4"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            currentRow++;

            // Group by room and sort by time
            var roomGroups = daySchedules
                .OrderBy(s => s.Room?.Name)
                .ThenBy(s => s.StartTime)
                .GroupBy(s => s.RoomId);

            if (!roomGroups.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "No classes scheduled for this day";
                worksheet.Range(currentRow, 1, currentRow, 6).Merge();
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.LightGray);
            }
            else
            {
                foreach (var roomGroup in roomGroups)
                {
                    var roomSchedules = roomGroup.OrderBy(s => s.StartTime).ToList();
                    var roomName = roomSchedules.First().Room?.Name ?? "Unknown";

                    foreach (var schedule in roomSchedules)
                    {
                        var isEvenRow = (currentRow - 5) % 2 == 0;
                        var bgColor = isEvenRow ? XLColor.FromHtml("#E7E6E6") : XLColor.White;

                        // Room
                        worksheet.Cell(currentRow, 1).Value = roomName;
                        worksheet.Cell(currentRow, 1).Style
                            .Fill.SetBackgroundColor(bgColor)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        // Time Slot
                        var timeSlot = $"{FormatTime(schedule.StartTime)} - {FormatTime(schedule.EndTime)}";
                        worksheet.Cell(currentRow, 2).Value = timeSlot;
                        worksheet.Cell(currentRow, 2).Style
                            .Fill.SetBackgroundColor(bgColor)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        // Subject Code
                        worksheet.Cell(currentRow, 3).Value = schedule.Subject?.SubjectCode ?? "N/A";
                        worksheet.Cell(currentRow, 3).Style
                            .Fill.SetBackgroundColor(bgColor)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        // Subject Title
                        worksheet.Cell(currentRow, 4).Value = schedule.Subject?.SubjectTitle ?? "N/A";
                        worksheet.Cell(currentRow, 4).Style
                            .Fill.SetBackgroundColor(bgColor)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        // Faculty
                        worksheet.Cell(currentRow, 5).Value = schedule.Faculty?.FullName ?? "N/A";
                        worksheet.Cell(currentRow, 5).Style
                            .Fill.SetBackgroundColor(bgColor)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        // Section
                        var sectionLabel = $"{schedule.ClassSection?.YearLevel}{schedule.ClassSection?.Section ?? ""}";
                        worksheet.Cell(currentRow, 6).Value = sectionLabel;
                        worksheet.Cell(currentRow, 6).Style
                            .Fill.SetBackgroundColor(bgColor)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                        currentRow++;
                    }

                    // Add a separator row between rooms
                    currentRow++;
                }
            }

            // Freeze header rows
            worksheet.SheetView.FreezeRows(5);
        }

        #endregion

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

        private static string GetEntityName(List<Schedule> schedules, string pov, string id)
        {
            if (!schedules.Any()) return id;

            return pov.ToLower() switch
            {
                "faculty" => schedules.FirstOrDefault()?.Faculty?.FullName ?? id,
                "class section" or "classsection" => schedules.FirstOrDefault()?.ClassSection?.Section ?? id,
                "room" => schedules.FirstOrDefault()?.Room?.Name ?? id,
                _ => id
            };
        }

        #endregion
    }
}