namespace Infrastructure.Models;

public class RoleAssignmentDetails
{
    public required string RoleName { get; set; }
    public required string? PrincipalName { get; set; }
    public required string? PrincipalId { get; set; }
    public required DateTimeOffset? CreatedOn { get; set; }
    public required string CreatedBy { get; set; }
    public required string Description { get; set; }
    public required string Scope { get; set; }
    public required string RoleId { get; set; }
}