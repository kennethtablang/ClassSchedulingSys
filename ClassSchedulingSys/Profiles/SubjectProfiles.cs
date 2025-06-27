using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using AutoMapper;

namespace ClassSchedulingSys.Profiles
{
    public class SubjectProfiles : Profile
    {
        public SubjectProfiles()
        {
            CreateMap<Subject, SubjectReadDTO>()
                .ForMember(dest => dest.CollegeCourseName, opt => opt.MapFrom(src => src.CollegeCourse.Name));

            CreateMap<SubjectCreateDTO, Subject>();
            CreateMap<SubjectUpdateDTO, Subject>();
        }
    }
}
