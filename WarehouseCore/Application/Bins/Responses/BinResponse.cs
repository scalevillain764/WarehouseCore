using Domain.Models;
namespace Application.Bins.Responses
{
    public sealed record BinResponse(
    Ulid Id,
    string Code,
    int RowNumber,
    int SectionNumber,
    int LevelNumber,
    string Type,
    decimal MaxWeight)
    {
        public BinResponse(Bin bin) :
            this(bin.Id, bin.BinCode, bin.Row, bin.Section, bin.Level, bin.Type.ToString(), (int)bin.Type)
        {}
    }
}
