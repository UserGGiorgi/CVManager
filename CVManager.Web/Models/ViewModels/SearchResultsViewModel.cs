using CVManager.Core.Entities;

namespace CVManager.Web.Models.ViewModels;

public class SearchResultsViewModel
{
    public string? Query { get; set; }
    public List<Position> Positions { get; set; } = new();
    public List<CV> CVs { get; set; } = new();
}