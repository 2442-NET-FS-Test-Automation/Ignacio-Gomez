using System.Diagnostics;
using BloomRush.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloomRush.Api.Controllers;
[ApiController]
[Route("orders")]

public class OrdersController : ControllerBase
{
    private readonly BloomRushDbContext _db;
    
    public OrdersController(BloomRushDbContext db)
    {
        _db = db;
    }

     [HttpDelete("{id:int}/cascade")]
     public async Task<ActionResult<DeleteOrderResponse>> DeleteOrder(int id, CancellationToken ct)
{
    var order = await _db.Orders
        .Include(order => order.Lines)
        .Include(order => order.Events)
        .Where(order => order.Id == id)
        .FirstOrDefaultAsync(ct);

    if (order == null)
    {
        return NotFound($"Order {id} was not found");
    }

    var deletedLines = order.Lines.Count;
    var deletedEvents = order.Events.Count;

    _db.FulfillmentEvents.RemoveRange(order.Events);
    _db.OrderLines.RemoveRange(order.Lines);
    _db.Orders.Remove(order);

    await _db.SaveChangesAsync(ct);

    return Ok(new DeleteOrderResponse(
        id,
        deletedLines,
        deletedEvents,
        "Order deleted"
    ));
}
}

public record DeleteOrderResponse(
    int OrderId,
    int DeletedLines,
    int DeletedEvents,
    string Message);