namespace ClassSchedulingSys.Models
{
    public class FacultySubjectAssignment
    {
        public int Id { get; set; }

        public string FacultyId { get; set; }
        public ApplicationUser Faculty { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public int ClassSectionId { get; set; }
        public ClassSection ClassSection { get; set; }
    }
}
