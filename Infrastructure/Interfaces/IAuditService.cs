namespace Infrastructure.Interfaces;

public interface IAuditService
{
    public Task<string> GetKeyOperationsPerformedAsync(int numOfDays);

    public Task<string> GetVaultOperationsPerformedAsync(int numOfDays);
}