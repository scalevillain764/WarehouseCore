using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Application.Bins.Responses;
using Infrastructure.ResponsePatterns;
using Domain.Models;
using Application.Bins.Requests;

namespace Application.Bins
{
    public class BinBuilder
    {
        private readonly WmsDbContext _context;
        public BinBuilder(WmsDbContext context)
        {
            _context = context;
        }

        // add section
        public async Task<Result<IReadOnlyList<BinResponse>>> AddSectionAsync(AddSectionRequest request, CancellationToken token)
        {
            var zone = await _context.Zones
               .FindAsync(request.zoneId, token);

            if (zone is null)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.NotFound, "Zone not found");

            var lastSection = await _context.Bins
                .AsNoTracking()
                .Where(x => x.WarehouseZoneId == request.zoneId)
                .Where(x => x.Row == request.rowNumber)
                .MaxAsync(x => (int?)x.Section, token);

            if (lastSection is null)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.NotFound, "Row not found");

            int newSection = lastSection.Value + 1;
            var newBins = new List<Bin>();

            if(zone.Code.Length < 3)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.Conflict, "Zone code is incorrect");

            var zonePrefix = zone.Code.AsSpan(0, 3);

            for(int l = 1; l <= request.levels; l++)
            {
                string binCode = $"{zonePrefix}-{request.rowNumber:D2}-{newSection:D2}-{l:D2}";
                var newBin = new Bin(binCode, request.rowNumber, newSection, l, request.zoneId, request.type);
                newBins.Add(newBin); 
            }

            _context.AddRange(newBins);
            await _context.SaveChangesAsync(token);

            return Result<IReadOnlyList<BinResponse>>.Success(newBins
                    .Select(x => new BinResponse(x))
                        .ToList());
        }

        // add level
        public async Task<Result<IReadOnlyList<BinResponse>>> AddLevelsAsync(AddLevelRequest request, CancellationToken token)
        {
            var zone = await _context.Zones
               .FindAsync(request.zoneId);

            if (zone is null)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.NotFound, "Zone not found");

            var lastLevel = await _context.Bins
                .AsNoTracking()
                .Where(x => x.WarehouseZoneId == request.zoneId)
                .Where(x => x.Row == request.rowNumber)
                .Where(x => x.Section == request.sectionNumber)
                .MaxAsync(x => (int?)x.Level, token);

            if (lastLevel is null)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.NotFound, "Section not found");

            int newLevel = (lastLevel ?? 0) + 1;
            var newBins = new List<Bin>();

            var zonePrefix = zone.Code
                .AsSpan(0, 3);

            for (int l = 0; l < request.levels; l++)
            {
                int levelNumber = newLevel + l;
                string binCode = $"{zonePrefix}-{request.rowNumber:D2}-{request.sectionNumber:D2}-{l:D2}";
                var newBin = new Bin(binCode, request.rowNumber, request.sectionNumber, levelNumber, request.zoneId, request.type);
                newBins.Add(newBin);
            }

            _context.AddRange(newBins);
            await _context.SaveChangesAsync(token);

            return Result<IReadOnlyList<BinResponse>>.Success(newBins
                    .Select(x => new BinResponse(x))
                        .ToList());
        }


        // add row
        public async Task<Result<IReadOnlyList<BinResponse>>> AddRowAsync(AddRowRequest request, CancellationToken token)
        {
            var zone = await _context.Zones
                .FindAsync(request.zoneId, token);

            if (zone is null)
                return Result<IReadOnlyList<BinResponse>>.Fail(ErrorType.NotFound, "Zone not found");

            var lastRow = await _context.Bins
                .AsNoTracking()
                .Where(x => x.WarehouseZoneId == request.zoneId)
                .MaxAsync(x => (int?)x.Row, token);

            int newRow = (lastRow ?? 0) + 1;

            var newBins = new List<Bin>();

            for(int s = 1; s <= request.sections; s++)
            {
                for(int l = 1; l <= request.levels; l++)
                {
                    string binCode = $"{zone.Code.Substring(0, 3)}-{newRow:D2}-{s:D2}-{l:D2}";
                    var newBin = new Bin(binCode, newRow, s, l, request.zoneId, request.type);
                    newBins.Add(newBin);
                }
            }

            _context.AddRange(newBins);
            await _context.SaveChangesAsync(token);

            return Result<IReadOnlyList<BinResponse>>.Success(newBins
                    .Select(x => new BinResponse(x))
                        .ToList());
        }
    }
}