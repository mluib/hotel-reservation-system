using HotelReservation.Application.DTOs;
using HotelReservation.Application.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly GetCustomerById _getCustomerById;
    private readonly GetCustomers _getCustomers;
    private readonly UpdateCustomer _updateCustomer;
    private readonly DeleteCustomer _deleteCustomer;
    private readonly GetCurrentCustomer _getCurrentCustomer;

    public CustomersController(
        GetCustomerById getCustomerById,
        GetCustomers getCustomers,
        UpdateCustomer updateCustomer,
        DeleteCustomer deleteCustomer,
        GetCurrentCustomer getCurrentCustomer)
    {
        _getCustomerById = getCustomerById;
        _getCustomers = getCustomers;
        _updateCustomer = updateCustomer;
        _deleteCustomer = deleteCustomer;
        _getCurrentCustomer = getCurrentCustomer;
    }

    // Customer-facing profile lookup (e.g. for the frontend nav bar), scoped to the
    // caller instead of an admin-only id lookup. Must be routed before "{id}" so
    // "me" isn't parsed as an id.
    [HttpGet("me")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMe()
    {
        var dto = await _getCurrentCustomer.ExecuteAsync();
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getCustomerById.ExecuteAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getCustomers.ExecuteAsync();
        return Ok(list);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(System.Guid id, UpdateCustomerRequest request)
    {
        await _updateCustomer.ExecuteAsync(id, request);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        try
        {
            await _deleteCustomer.ExecuteAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
