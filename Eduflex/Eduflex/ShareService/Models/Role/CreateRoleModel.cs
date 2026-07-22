namespace ShareService.Models.Role
{
    public class CreateRoleModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> PermissionIds { get; set; } = new();
    }
}
