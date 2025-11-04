// ClassSchedulingSys/DTO/ScheduleDto - FIXED to handle day as string or enum
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassSchedulingSys.DTO
{
    public class ScheduleCreateDto
    {
        [Required]
        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string FacultyId { get; set; } = string.Empty;

        public int RoomId { get; set; }

        public int SubjectId { get; set; }

        public int ClassSectionId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ScheduleUpdateDto
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string FacultyId { get; set; } = string.Empty;

        public int RoomId { get; set; }

        public int SubjectId { get; set; }

        public int ClassSectionId { get; set; }

        public bool IsActive { get; set; }
    }

    public class ScheduleReadDto
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public double Duration { get; set; }

        public string FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;

        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;

        public int SubjectId { get; set; }
        public string SubjectTitle { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int SubjectUnits { get; set; }
        public int SubjectHours { get; set; }
        public string SubjectColor { get; set; } = "#999999";

        public int ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;

        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public string SchoolYearLabel { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class ConflictCheckResultDto
    {
        public bool HasConflict { get; set; }
        public List<string> ConflictingResources { get; set; } = new();
    }

    // ✅ FIXED: Accept day as either string or enum
    public class ScheduleConflictCheckDto
    {
        public int? Id { get; set; } // Optional: if editing

        [Required]
        [JsonConverter(typeof(DayOfWeekConverter))]
        public object Day { get; set; } = DayOfWeek.Monday;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public string FacultyId { get; set; } = string.Empty;

        [Required]
        public int RoomId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int ClassSectionId { get; set; }
    }

    // ✅ Custom converter to handle day as string or enum
    public class DayOfWeekConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dayString = reader.GetString();
                if (Enum.TryParse<DayOfWeek>(dayString, true, out var day))
                {
                    return day;
                }
                throw new JsonException($"Invalid day of week: {dayString}");
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return (DayOfWeek)reader.GetInt32();
            }

            throw new JsonException("Day must be a string or number");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is DayOfWeek day)
            {
                writer.WriteStringValue(day.ToString());
            }
            else if (value is string str)
            {
                writer.WriteStringValue(str);
            }
            else
            {
                throw new JsonException("Day must be a DayOfWeek enum or string");
            }
        }
    }
}