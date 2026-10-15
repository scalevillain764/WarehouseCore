using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Application.Bins.Responses;
using Infrastructure.ResponsePatterns;

namespace Domain.Models
{
    public class BinBuilder
    {
        private readonly AppDbContext _context;
        public BinBuilder(AppDbContext context)
        {
            _context = context;
        }
    
        // add row
        public async Task<Result<IReadOnlyList<BinResponse>>> AddRowAsync(Ulid ZoneId, int sections, int levels, BinTypeByCapacity type)
        {
            var zone = await _context.Zones
                .FindAsync(ZoneId);

            if (zone is null)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.NotFound, "Zone not found");

            var lastRow = await _context.Bins
                .Where(x => x.WarehouseZoneId == ZoneId)
                .MaxAsync(x => (int?)x.Row);

            int newRow = (lastRow ?? 0) + 1;

            var newBins = new List<Bin>();

            for(int s = 1; s <= sections; s++)
            {
                for(int l = 1; l <= levels; l++)
                {
                    string binCode = $"{zone.Code.Substring(0, 3)}-{newRow:D2}-{s:D2}-{l:D2}";
                    var newBin = new Bin(binCode, newRow, s, l, ZoneId, type);
                    newBins.Add(newBin);
                }
            }

            _context.AddRange(newBins);
            await _context.SaveChangesAsync();

            return Result<IReadOnlyList<BinResponse>>.Success(newBins
                    .Select(x => new BinResponse(x))
                        .ToList());
        }
    }
}