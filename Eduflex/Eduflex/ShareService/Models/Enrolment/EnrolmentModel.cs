using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShareService.Models.Address;
using ShareService.Models.Common;
using ShareService.Enums.Roles;

namespace ShareService.Models.Enrolment
{
    public class EnrolmentModel : AuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        // ----- Ownership / linkage (server-owned, never touched by ApplyEditableFields) -----
        [BsonElement("ownerUserId")]
        public string OwnerUserId { get; set; } = string.Empty;

        [BsonElement("studentUserId")]
        public string StudentUserId { get; set; } = string.Empty;

        [BsonElement("studentApplicationId")]
        public string? StudentApplicationId { get; set; }

        [BsonElement("enquiryId")]
        public string? EnquiryId { get; set; }

        // ----- Tab 1: Student information -----
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("middleName")]
        public string? MiddleName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [BsonElement("gender")]
        public string? Gender { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("mobile")]
        public string Mobile { get; set; } = string.Empty;

        [BsonElement("nationality")]
        public string? Nationality { get; set; }

        [BsonElement("passportNumber")]
        public string? PassportNumber { get; set; }

        [BsonElement("hometownAddress")]
        public AddressModel? HometownAddress { get; set; }

        [BsonElement("currentAddress")]
        public AddressModel? CurrentAddress { get; set; }

        [BsonElement("emergencyContact")]
        public EmergencyContactModel? EmergencyContact { get; set; }

        // ----- Tab 2: Enrolment form -----
        [BsonElement("educationPartnerId")]
        public string? EducationPartnerId { get; set; }

        [BsonElement("courseId")]
        public string? CourseId { get; set; }

        [BsonElement("intake")]
        public string? Intake { get; set; }

        [BsonElement("studyMode")]
        public string? StudyMode { get; set; }

        [BsonElement("campus")]
        public string? Campus { get; set; }

        [BsonElement("commencementDate")]
        public DateTime? CommencementDate { get; set; }
      
        [BsonElement("actualCommencementDate")]
        public DateTime? ActualCommencementDate { get; set; }

        [BsonElement("expectedCompletionDate")]
        public DateTime? ExpectedCompletionDate { get; set; }

        [BsonElement("fundingSource")]
        public string? FundingSource { get; set; }

        [BsonElement("visaStatus")]
        public string? VisaStatus { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = EnrolmentEnums.Draft.ToString();

        [BsonElement("notes")]
        public string? Notes { get; set; }

        // Prefilled from the linked Course's TuitionFee ("Total Tuition") when an
        // enrolment is created/updated and no fee is set yet, but stays independently
        // editable afterwards — see EnrolmentService's one-time-prefill logic.
        [BsonElement("tuitionFee")]
        public decimal? TuitionFee { get; set; }

        // Server-owned, derived from the linked EducationPartner.BusinessPartnerId —
        // never touched by ApplyEditableFields, only ever set inside EnrolmentService
        // whenever EducationPartnerId is set/changed. Hidden field in the UI.
        [BsonElement("businessPartnerId")]
        public string? BusinessPartnerId { get; set; }

        // ----- Tab 3/4/5: append-only sub-collections -----
        [BsonElement("documents")]
        public List<EnrolmentDocumentModel> Documents { get; set; } = new();

        [BsonElement("communications")]
        public List<EnrolmentCommunicationModel> Communications { get; set; } = new();

        // Dynamic Form requests/responses on this enrolment — one entry per requested
        // form. See EnrolmentFormResponseModel for why this is embedded rather than a
        // separate top-level collection.
        [BsonElement("formResponses")]
        public List<EnrolmentFormResponseModel> FormResponses { get; set; } = new();

        [BsonElement("auditTrail")]
        public List<EnrolmentAuditEntryModel> AuditTrail { get; set; } = new();

        // ----- VISA Process tab: fixed 6-step workflow, never touched by ApplyEditableFields —
        // mutated only via EnrolmentService.SaveVisaStepDraftAsync/CompleteVisaStepAsync. -----
        [BsonElement("visaProcessSteps")]
        public List<VisaProcessStepModel> VisaProcessSteps { get; set; } = new();

        // ----- Multiple concurrent course applications (Enrolment Form step's nested
        // sub-panels), never touched by ApplyEditableFields — mutated only via
        // EnrolmentService.AddCourseApplicationAsync/SetCourseApplicationStatusAsync/
        // FinalizeCourseApplicationAsync. -----
        [BsonElement("courseApplications")]
        public List<CourseApplicationModel> CourseApplications { get; set; } = new();

        // Null means "use Settings.MaxApplicationsPerStudent" — set explicitly the first
        // time staff save the override control, and from then on capped at (never above)
        // whatever the current global setting is. See EnrolmentService.SetMaxApplicationsOverrideAsync.
        [BsonElement("maxApplicationsOverride")]
        public int? MaxApplicationsOverride { get; set; }
    }
}
