using CVManager.Core.Entities;

namespace CVManager.Web.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Position> LatestPositions { get; set; } = new();
        public List<Position> PopularPositions { get; set; } = new();
        public List<string> TagCloud { get; set; } = new();
        public int TotalCVs { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalRecruiters { get; set; }
        public int NewCVs24h { get; set; }
    }
}
