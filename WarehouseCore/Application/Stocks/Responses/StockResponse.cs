using Domain.Models;
namespace Application.Stocks.Responses;
public sealed record StockResponse(Ulid Id, Ulid BinId, string BinCode, Ulid ProductId,
    string ProductName, string ProductSKU, int Quantity)
{
    public StockResponse(Stock st) : this(st.Id, st.BinId, st.Bin.BinCode, 
        st.ProductId, st.Product.Name, st.Product.SKU, st.Quantity)
    { }
}