namespace Eduflex.DTOs.EducationPartner
{
    public class EducationPartnerFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? SearchTerm { get; set; }
    }
}
