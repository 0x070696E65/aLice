using SymbolSdk.Symbol;

namespace aLice.Services;

public class Metal
{
    public readonly SymbolService symbol;
    public Metal(SymbolSdk.Symbol.Network _network)
    {
        var config = new SymbolServiceConfig("");
        var network = new MetalForSymbol.models.Network(_network);
        symbol = new SymbolService(config);
        symbol.Init(network);
    }
    public List<AggregateCompleteTransactionV3> SignedAggregateCompleteTxBatches(List<IBaseTransaction> _txs, KeyPair _signerKeyPair, SymbolSdk.Symbol.Network _network)
    {
        return symbol.BuildSignedAggregateCompleteTxBatches(_txs, _signerKeyPair);
    }
}