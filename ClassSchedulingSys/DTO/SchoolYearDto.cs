namespace ClassSchedulingSys.DTO
{
    public class SchoolYearDto
    {
        public int Id { get; set; }
        public int StartYear { get; set; }
        public int EndYear { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsArchived { get; set; }
        public string Label => $"{StartYear}-{EndYear}";
    }

    public class CreateSchoolYearDto
    {
        public int StartYear { get; set; }
        public int EndYear { get; set; }
    }
}
