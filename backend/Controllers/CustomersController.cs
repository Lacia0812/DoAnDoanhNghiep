using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin,Sales")]
public sealed class CustomersController(AppDbContext db, OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<CustomerDto>> GetAll(
        int page = 1, int pageSize = 10, string? search = null,
        CustomerType? customerType = null, bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Customers.AsNoTracking().Include(x => x.Orders).AsQueryable();
        if (!includeInactive || !User.IsInRole(UserRole.Admin.ToString()))
            query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.CustomerName.Contains(term) ||
                x.Phone.Contains(term) ||
                x.CustomerCode.Contains(term));
        }
        if (customerType.HasValue)
            query = query.Where(x => x.CustomerType == customerType);

        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderBy(x => x.CustomerName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<CustomerDto>.Create(
            entities.Select(x => x.ToDto()).ToArray(), count, page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<CustomerDto> Get(int id, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.AsNoTracking().Include(x => x.Orders)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw CustomerNotFound();
        if (!customer.IsActive && !User.IsInRole(UserRole.Admin.ToString()))
            throw CustomerNotFound();
        return customer.ToDto();
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customerType = request.CustomerType ?? throw CustomerTypeRequired();
        var code = request.CustomerCode.Trim().ToUpperInvariant();
        if (await db.Customers.AnyAsync(x => x.CustomerCode == code, cancellationToken))
            throw DuplicateCode();
        var entity = new Customer
        {
            CustomerCode = code,
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            Email = Clean(request.Email),
            Address = request.Address.Trim(),
            CustomerType = customerType,
            Note = Clean(request.Note),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<CustomerDto> Update(
        int id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customerType = request.CustomerType ?? throw CustomerTypeRequired();
        var entity = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw CustomerNotFound();
        var code = request.CustomerCode.Trim().ToUpperInvariant();
        if (await db.Customers.AnyAsync(
                x => x.Id != id && x.CustomerCode == code, cancellationToken))
            throw DuplicateCode();
        entity.CustomerCode = code;
        entity.CustomerName = request.CustomerName.Trim();
        entity.Phone = request.Phone.Trim();
        entity.Email = Clean(request.Email);
        entity.Address = request.Address.Trim();
        entity.CustomerType = customerType;
        entity.Note = Clean(request.Note);
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await db.Customers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw CustomerNotFound();
        entity.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/orders")]
    public async Task<PagedResult<OrderDto>> Orders(
        int id, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == id, cancellationToken))
            throw CustomerNotFound();
        return await orderService.ListAsync(
            page, pageSize, null, null, id, cancellationToken);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static BusinessException CustomerNotFound() =>
        new(StatusCodes.Status404NotFound, "Không tìm thấy khách hàng.", "customer_not_found");
    private static BusinessException DuplicateCode() =>
        new(StatusCodes.Status409Conflict, "Mã khách hàng đã tồn tại.", "duplicate_customer_code");
    private static BusinessException CustomerTypeRequired() =>
        new(StatusCodes.Status400BadRequest,
            "Loại khách hàng là bắt buộc.", "customer_type_required");
}
