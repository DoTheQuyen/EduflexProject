using Eduflex.DTOs.EducationPartner;
using ShareService.Models.Course;

namespace Eduflex.Mapping.EducationPartner
{
    public static class CourseMappingExtension
    {
        public static CourseSearchFilter ToFilter(this CourseSearchFilterDto dto)
        {
            return new CourseSearchFilter
            {
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                CourseName = dto.CourseName,
                UniName = dto.UniName,
                Country = dto.Country,
                Intake = dto.Intake
            };
        }

        public static CourseModel ToModel(this CreateCourseDto dto)
        {
            return new CourseModel
            {
                EducationPartnerId = dto.EducationPartnerId,
                CourseName = dto.CourseName,
                Intakes = dto.Intakes,
                StudyModes = dto.StudyModes,
                Campuses = dto.Campuses,
                TuitionFee = dto.TuitionFee,
                TotalTuitionFee = dto.TotalTuitionFee,
                TuitionCurrency = dto.TuitionCurrency,
                CourseDurationMonths = dto.CourseDurationMonths,
                CommissionBaseRate = dto.CommissionBaseRate
            };
        }

        public static CourseDto ToDto(this CourseModel model)
        {
            return new CourseDto
            {
                Id = model.Id,
                EducationPartnerId = model.EducationPartnerId,
                CourseName = model.CourseName,
                Intakes = model.Intakes,
                StudyModes = model.StudyModes,
                Campuses = model.Campuses,
                TuitionFee = model.TuitionFee,
                TotalTuitionFee = model.TotalTuitionFee,
                TuitionCurrency = model.TuitionCurrency,
                CourseDurationMonths = model.CourseDurationMonths,
                CommissionBaseRate = model.CommissionBaseRate
            };
        }

        public static CourseSearchResultDto ToSearchResultDto(this CourseSearchResult result)
        {
            return new CourseSearchResultDto
            {
                Id = result.Course.Id,
                CourseName = result.Course.CourseName,
                Intakes = result.Course.Intakes,
                TuitionFee = result.Course.TuitionFee,
                TuitionCurrency = result.Course.TuitionCurrency,
                EducationPartnerId = result.Partner.Id,
                UniName = result.Partner.Name,
                Country = result.Partner.Country
            };
        }
    }
}
