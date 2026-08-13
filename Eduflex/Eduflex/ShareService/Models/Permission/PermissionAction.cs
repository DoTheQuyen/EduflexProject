namespace ShareService.Models.Permission
{
    public enum PermissionAction
    {
        View,
        Add,
        Edit,
        Delete,
        Reassign,
        // Tasks' "All Tasks" (department-scoped, Manager/Admin-only) permission action —
        // see migration 043. Bson-deserialized by exact name (BsonRepresentation.String),
        // so this enum must have a member for every "action" string ever written to the
        // Permissions collection, or ANY read that touches that document (not just the
        // one permission itself) throws and PermissionCatalog.GetByIdsAsync's whole
        // batch fails — which is what silently emptied every permission for every module,
        // not just Tasks, until this member existed.
        ViewAll
    }
}
