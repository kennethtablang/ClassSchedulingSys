// ClassSchedulingSys/Services/GridScheduleExcelService.cs
using ClassSchedulingSys.Models;
using ClosedXML.Excel;

namespace ClassSchedulingSys.Services
{
    /// <summary>
    /// Generates Excel files in grid format (time slots x rooms) similar to visual timetables
    /// </summary>
    public class GridScheduleExcelService
    {
        /// <summary>
        /// Generates a grid-style schedule Excel with time slots as rows and rooms as columns
        /// </summary>
        public byte[] GenerateGridScheduleExcel(
            List<Schedule> schedules,
            List<Room> rooms,
            string semesterLabel,
            string schoolYearLabel)
        {
            using var workbook = new XLWorkbook();

            // Create a worksheet for each day
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
                CreateGridWorksheet(workbook, day.ToString(), daySchedules, rooms, semesterLabel, schoolYearLabel);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void CreateGridWorksheet(
            XLWorkbook workbook,
            string dayName,
            List<Schedule> daySchedules,
            List<Room> rooms,
            string semesterLabel,
            string schoolYearLabel)
        {
            var worksheet = workbook.Worksheets.Add(dayName);

            // Generate time slots from 7:00 AM to 9:00 PM in 30-minute intervals
            var timeSlots = GenerateTimeSlots(TimeSpan.FromHours(7), TimeSpan.FromHours(21), 30);

            int currentRow = 1;
            int currentCol = 1;

            // === HEADER ===
            worksheet.Cell(currentRow, 1).Value = "Classroom Schedule";
            worksheet.Range(currentRow, 1, currentRow, rooms.Count + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = $"{semesterLabel} • SY {schoolYearLabel}";
            worksheet.Range(currentRow, 1, currentRow, rooms.Count + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetFontSize(10)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = dayName.ToUpper();
            worksheet.Range(currentRow, 1, currentRow, rooms.Count + 1).Merge();
            worksheet.Cell(currentRow, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.Yellow);
            currentRow += 2;

            // === COLUMN HEADERS (Rooms) ===
            int headerRow = currentRow;

            // First column for time
            worksheet.Cell(headerRow, 1).Value = "TIME";
            worksheet.Cell(headerRow, 1).Style
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(XLColor.LightGray)
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
            worksheet.Column(1).Width = 12;

            // Room columns
            for (int i = 0; i < rooms.Count; i++)
            {
                int colIndex = i + 2;
                var room = rooms[i];

                worksheet.Cell(headerRow, colIndex).Value = room.Name;
                worksheet.Cell(headerRow, colIndex).Style
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
                worksheet.Column(colIndex).Width = 15;
            }

            currentRow++;

            // === TIME SLOT ROWS ===
            foreach (var timeSlot in timeSlots)
            {
                // Time column
                worksheet.Cell(currentRow, 1).Value = $"{FormatTime(timeSlot.Start)} - {FormatTime(timeSlot.End)}";
                worksheet.Cell(currentRow, 1).Style
                    .Font.SetFontSize(9)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#F0F0F0"));

                // Room columns
                for (int i = 0; i < rooms.Count; i++)
                {
                    int colIndex = i + 2;
                    var room = rooms[i];

                    // Find schedules that overlap with this time slot in this room
                    var matchingSchedules = daySchedules.Where(s =>
                        s.RoomId == room.Id &&
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

                            // Build cell content
                            var subjectCode = schedule.Subject?.SubjectCode ?? "N/A";
                            //var subjectTitle = schedule.Subject?.SubjectTitle ?? "N/A";
                            var facultyName = schedule.Faculty?.FullName ?? "N/A";
                            var section = $"{schedule.ClassSection?.CollegeCourse?.Code ?? ""} {schedule.ClassSection?.YearLevel}{schedule.ClassSection?.Section ?? ""}".Trim();
                            //var timeRange = $"{FormatTime(schedule.StartTime)} - {FormatTime(schedule.EndTime)}";

                            cell.Value = $"{subjectCode}\n{facultyName}\n{section}";

                            // Apply styling
                            var bgColor = GetColorFromHex(schedule.Subject?.Color ?? "#CCCCCC");
                            cell.Style
                                .Font.SetFontSize(8)
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

            // Freeze panes (header rows and time column)
            worksheet.SheetView.FreezeRows(headerRow);
            worksheet.SheetView.FreezeColumns(1);
        }

        #region Helper Methods

        /// <summary>
        /// Generate time slots for the grid
        /// </summary>
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

        /// <summary>
        /// Calculate how many time slots a schedule should span
        /// </summary>
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

        /// <summary>
        /// Format time in 12-hour format
        /// </summary>
        private string FormatTime(TimeSpan time)
        {
            var hours = time.Hours;
            var minutes = time.Minutes;
            var period = hours >= 12 ? "PM" : "AM";
            var displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;

            return $"{displayHours}:{minutes:D2} {period}";
        }

        /// <summary>
        /// Convert hex color string to XLColor
        /// </summary>
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