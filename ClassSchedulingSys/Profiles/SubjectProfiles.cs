using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using AutoMapper;

namespace ClassSchedulingSys.Profiles
{
    public class SubjectProfiles : Profile
    {
        public SubjectProfiles()
        {
            CreateMap<Subject, SubjectReadDto>()
                .ForMember(dest => dest.CollegeCourseName, opt => opt.MapFrom(src => src.CollegeCourse.Name));

            CreateMap<SubjectCreateDto, Subject>();
            CreateMap<SubjectUpdateDto, Subject>();
        }
    }
}
