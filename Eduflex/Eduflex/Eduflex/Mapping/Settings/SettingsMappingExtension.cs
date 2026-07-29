using Eduflex.DTOs.Settings;
using ShareService.Models.Settings;

namespace Eduflex.Mapping.Settings
{
    public static class SettingsMappingExtension
    {
        public static SettingsDto ToDto(this SettingsModel model)
        {
            return new SettingsDto
            {
                FeedbackDefaultLatestCount = model.FeedbackDefaultLatestCount,
                CoursePromotionDefaultLatestCount = model.CoursePromotionDefaultLatestCount,
                DocumentUpload = new DocumentUploadSettingsDto
                {
                    Default = model.DocumentUpload.Default.ToDto(),
                    Other = model.DocumentUpload.Other.ToDto()
                },
                ImageUpload = model.ImageUpload.ToDto(),
                ContractUpload = model.ContractUpload.ToDto(),
                EnrolmentUpload = model.EnrolmentUpload.ToDto()
            };
        }

        public static UploadLimitDto ToDto(this UploadLimit model)
        {
            return new UploadLimitDto
            {
                MaxSizeMB = model.MaxSizeMB,
                AllowedExtensions = model.AllowedExtensions,
                MaxFileCount = model.MaxFileCount
            };
        }

        public static SettingsModel ToModel(this UpdateSettingsDto dto)
        {
            return new SettingsModel
            {
                FeedbackDefaultLatestCount = dto.FeedbackDefaultLatestCount,
                CoursePromotionDefaultLatestCount = dto.CoursePromotionDefaultLatestCount,
                DocumentUpload = new DocumentUploadSettings
                {
                    Default = dto.DocumentUpload.Default.ToModel(),
                    Other = dto.DocumentUpload.Other.ToModel()
                },
                ImageUpload = dto.ImageUpload.ToModel(),
                ContractUpload = dto.ContractUpload.ToModel(),
                EnrolmentUpload = dto.EnrolmentUpload.ToModel()
            };
        }

        public static UploadLimit ToModel(this UploadLimitDto dto)
        {
            return new UploadLimit
            {
                MaxSizeMB = dto.MaxSizeMB,
                AllowedExtensions = dto.AllowedExtensions,
                MaxFileCount = dto.MaxFileCount
            };
        }
    }
}
