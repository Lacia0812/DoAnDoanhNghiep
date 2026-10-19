using System.Data;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = "Admin,Sales,Warehouse")]
public sealed class ProductsController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetAll(
        int page = 1, int pageSize = 10, string? search = null, int? categoryId = null,
        bool? lowStock = null, bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var query = db.Products.AsNoTracking().Include(x => x.Category).AsQueryable();
        if (!includeInactive || !User.IsInRole(UserRole.Admin.ToString()))
            query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.ProductName.Contains(term) || x.ProductCode.Contains(term));
        }
        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId);
        if (lowStock.HasValue)
            query = lowStock.Value
                ? query.Where(x => x.StockQuantity < x.MinimumStock)
                : query.Where(x => x.StockQuantity >= x.MinimumStock);

        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderBy(x => x.ProductName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<ProductDto>.Create(
            entities.Select(x => x.ToDto()).ToArray(), count, page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().Include(x => x.Category)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ProductNotFound();
        if (!product.IsActive && !User.IsInRole(UserRole.Admin.ToString()))
            throw ProductNotFound();
        return product.ToDto();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request, CancellationToken cancellationToken)
    {
        var code = request.ProductCode.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(x => x.ProductCode == code, cancellationToken))
            throw DuplicateCode();
        if (!await db.ProductCategories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Loại sản phẩm không tồn tại.", "invalid_category");
        ValidateContainer(request.RequiresContainer, request.ContainerDeposit);

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var product = new Product
        {
            ProductCode = code,
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            Capacity = request.Capacity.Trim(),
            Unit = request.Unit.Trim(),
            Price = request.Price,
            RequiresContainer = request.RequiresContainer,
            ContainerDeposit = request.RequiresContainer ? request.ContainerDeposit : 0,
            StockQuantity = request.InitialStock,
            MinimumStock = request.MinimumStock,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        if (request.InitialStock > 0)
        {
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                TransactionType = InventoryTransactionType.StockIn,
                Quantity = request.InitialStock,
                QuantityBefore = 0,
                QuantityAfter = request.InitialStock,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                Note = "Tồn kho ban đầu"
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        var dto = await Get(product.Id, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, dto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ProductDto> Update(
        int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ProductNotFound();
        var code = request.ProductCode.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(
                x => x.Id != id && x.ProductCode == code, cancellationToken))
            throw DuplicateCode();
        if (!await db.ProductCategories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Loại sản phẩm không tồn tại.", "invalid_category");
        ValidateContainer(request.RequiresContainer, request.ContainerDeposit);

        product.ProductCode = code;
        product.ProductName = request.ProductName.Trim();
        product.CategoryId = request.CategoryId;
        product.Capacity = request.Capacity.Trim();
        product.Unit = request.Unit.Trim();
        product.Price = request.Price;
        product.RequiresContainer = request.RequiresContainer;
        product.ContainerDeposit = request.RequiresContainer ? request.ContainerDeposit : 0;
        product.MinimumStock = request.MinimumStock;
        product.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw ProductNotFound();
        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Warehouse")]
    [HttpPost("{id:int}/add-stock")]
    public async Task<ProductDto> AddStock(
        int id, AddStockRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(
            x => x.Id == id && x.IsActive, cancellationToken) ?? throw ProductNotFound();
        var before = product.StockQuantity;
        try
        {
            product.StockQuantity = checked(product.StockQuantity + request.Quantity);
        }
        catch (OverflowException)
        {
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Số lượng tồn kho vượt quá giới hạn.", "stock_overflow");
        }
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = product.Id,
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = request.Quantity,
            QuantityBefore = before,
            QuantityAfter = product.StockQuantity,
            CreatedByUserId = currentUser.Id,
            CreatedAt = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(request.Note) ? "Nhập thêm kho" : request.Note.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    private static (int, int) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
    private static BusinessException ProductNotFound() =>
        new(StatusCodes.Status404NotFound, "Không tìm thấy sản phẩm.", "product_not_found");
    private static BusinessException DuplicateCode() =>
        new(StatusCodes.Status409Conflict, "Mã sản phẩm đã tồn tại.", "duplicate_product_code");
    private static void ValidateContainer(bool requiresContainer, decimal deposit)
    {
        if (requiresContainer && deposit <= 0)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Tiền cọc vỏ bình phải lớn hơn 0.", "invalid_container_deposit");
    }
}
