using Infrastructure.Interfaces;
using Infrastructure.Models;

namespace Infrastructure.TransferBlobStrategies;

public class SpecifiedTransferBlobStrategy : ITransferBlobStrategy
{
    private readonly KeyTransferBlob _transferBlobAsJson;

    public SpecifiedTransferBlobStrategy(KeyTransferBlob transferBlobAsJson)
    {
        _transferBlobAsJson = transferBlobAsJson;
    }

    public KeyTransferBlob GenerateTransferBlob()
    {
        return _transferBlobAsJson;
    }
}