using CatSdk.Symbol;

namespace aLice.Services;

public class Metal
{
    private readonly SymbolService symbol;
    public Metal(CatSdk.Symbol.Network _network)
    {
        var config = new SymbolServiceConfig("");
        var network = new MetalForSymbol.models.Network(_network);
        symbol = new SymbolService(config);
        symbol.Init(network);
    }
    public List<AggregateCompleteTransactionV2> SignedAggregateCompleteTxBatches(List<IBaseTransaction> _txs, KeyPair _signerKeyPair, CatSdk.Symbol.Network _network)
    {
        return symbol.BuildSignedAggregateCompleteTxBatches(_txs, _signerKeyPair);
    }
}