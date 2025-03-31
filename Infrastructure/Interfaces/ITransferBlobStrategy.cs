using Infrastructure.Models;

namespace Infrastructure.Interfaces;

public interface ITransferBlobStrategy
{
    public KeyTransferBlob GenerateTransferBlob();
}