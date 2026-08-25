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

    // Deliberately no [HttpPost] here: customers are only ever created via
    // AccountController.Register, atomically with the linked IdentityUser. A bare create
    // endpoint on this controller would let someone create an orphaned Customer row with no
    // login, which ownership checks elsewhere (keyed on IdentityUserId) aren't designed to
    // handle.

    /// <summary>
    /// Customer-facing profile lookup (e.g. for the frontend nav bar), scoped to the
    /// caller instead of an admin-only id lookup. The literal "mine" segment always
    /// takes precedence over "{id}" in ASP.NET Core's route matching, so this doesn't
    /// depend on its position relative to <see cref="GetById"/> below.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine()
    {
        var dto = await _getCurrentCustomer.ExecuteAsync();
        return Ok(dto);
    }

    /// <summary>
    /// Admin lookup of a single customer by id.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getCustomerById.ExecuteAsync(id);
        return Ok(dto);
    }

    /// <summary>
    /// Lists every registered customer (admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IEnumerable<CustomerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getCustomers.ExecuteAsync();
        return Ok(list);
    }

    /// <summary>
    /// Updates a customer's profile (admin only).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(System.Guid id, UpdateCustomerRequest request)
    {
        await _updateCustomer.ExecuteAsync(id, request);
        return NoContent();
    }

    /// <summary>
    /// Removes a customer along with its linked login (admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteCustomer.ExecuteAsync(id);
        return NoContent();
    }
}
