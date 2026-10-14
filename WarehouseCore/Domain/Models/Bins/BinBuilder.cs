using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Domain.Models
{
    public class BinBuilder
    {
        private readonly AppDbContext _context;
        public BinBuilder(AppDbContext context)
        {
            _context = context;
        }

        // add new tomorrow

        public async Task<List<Bin>?> GenerateNewBinsAsync(WarehouseZone zone, int startRowNumber, int rowsCount, int startSectionNumber, int sectionsCount, 
            int startLevelNumber, int levelsCount)
        {
            var generatedBins = new List<Bin>();

            var zoneExists = await _context.Zones
                .AnyAsync(x => x.Id == zone.Id);

            if (!zoneExists)
                return null;

            var binsInSection = await _context.Bins
                .Where(x => x.WarehouseZoneId == zone.Id)
                .Where(x => x.Row == startRowNumber)
                .Where(x => x.Section == startSectionNumber)
                .ToListAsync();

            if (binsInSection.Count == 0)
            {
                if (startLevelNumber != 1)
                    return null;
            }
            else
            {
                int maxLevel = binsInSection.Max(x => x.Level);
                int nextLevel = maxLevel + 1;

                if (nextLevel != startLevelNumber)
                    return null;
            }

            for(int r = startRowNumber; r <= startRowNumber + rowsCount - 1; r++)
            {
                for(int s = startSectionNumber; s <= startSectionNumber + sectionsCount - 1; s++)
                {
                    for(int l = startLevelNumber; l <= startLevelNumber + levelsCount - 1; l++)
                    {
                        string binCode = $"{zone.Code.Substring(0, 3)}-{r:D2}-{s:D2}-{l:D2}";
                        generatedBins.Add(new Bin(binCode, r, s, l, zone.Id));
                    }
                }
            }

            return generatedBins;          
        }
    }
}