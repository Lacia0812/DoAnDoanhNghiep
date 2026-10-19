using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/product-categories")]
[Authorize]
public sealed class ProductCategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<ProductCategoryDto>> GetAll(
        CancellationToken cancellationToken) =>
        await db.ProductCategories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ProductCategoryDto(x.Id, x.Name, x.Description))
            .ToListAsync(cancellationToken);
}
