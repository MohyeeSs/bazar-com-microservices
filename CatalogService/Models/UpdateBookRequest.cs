namespace CatalogService.Models;

public class UpdateBookRequest
{
    public double? Price { get; set; }

    public int? QuantityChange { get; set; }
}
