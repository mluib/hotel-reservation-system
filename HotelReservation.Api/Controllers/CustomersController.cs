using HotelReservation.Application.DTOs;
using HotelReservation.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CreateCustomer _createCustomer;
    private readonly GetCustomerById _getCustomerById;
    private readonly GetCustomers _getCustomers;
    private readonly UpdateCustomer _updateCustomer;
    private readonly DeleteCustomer _deleteCustomer;

    public CustomersController(
        CreateCustomer createCustomer,
        GetCustomerById getCustomerById,
        GetCustomers getCustomers,
        UpdateCustomer updateCustomer,
        DeleteCustomer deleteCustomer)
    {
        _createCustomer = createCustomer;
        _getCustomerById = getCustomerById;
        _getCustomers = getCustomers;
        _updateCustomer = updateCustomer;
        _deleteCustomer = deleteCustomer;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request)
    {
        await _createCustomer.ExecuteAsync(request);
        return Ok();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getCustomerById.ExecuteAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getCustomers.ExecuteAsync();
        return Ok(list);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(System.Guid id, UpdateCustomerRequest request)
    {
        await _updateCustomer.ExecuteAsync(id, request);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteCustomer.ExecuteAsync(id);
        return Ok();
    }
}
