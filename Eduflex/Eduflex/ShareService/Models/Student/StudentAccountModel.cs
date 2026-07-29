namespace ShareService.Models.Student
{
    // Bundles a Student profile with the display fields that only live on its
    // paired User (Mobile/IsActive/LastLogin) — Student itself carries no
    // active/archived flag, User.IsActive is the single source of truth.
    public class StudentAccountModel
    {
        public StudentModel Student { get; set; } = null!;
        public string Mobile { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
