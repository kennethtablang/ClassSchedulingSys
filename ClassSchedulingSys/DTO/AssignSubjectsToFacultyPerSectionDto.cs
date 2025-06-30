namespace ClassSchedulingSys.DTO
{
    public class AssignSubjectsToFacultyPerSectionDto
    {
        public string FacultyId { get; set; }
        public List<SubjectSectionAssignment> Assignments { get; set; }
    }

    public class SubjectSectionAssignment
    {
        public int SubjectId { get; set; }
        public int ClassSectionId { get; set; }
    }

    public class AssignedSubjectInfoDto
    {
        public int SubjectId { get; set; }
        public int ClassSectionId { get; set; }
        public string FacultyName { get; set; }
    }
}
