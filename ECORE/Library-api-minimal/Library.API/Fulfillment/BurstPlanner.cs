using Library.Data.Entities;
namespace Library.API.Fulfillment;

using Library.Date.Entities;
using Microsoft.EntityFrameworkCore;

public class BurstPlanner
{
    //Method to plan fulfillment order
    public IReadOnlyList<int> OrderByPriority(IEnumerable<Order> orders)
    {
        //We could our own implementation on this - we wont
        //We can use PriorityQueue - allos FIFO
        PriorityQueue<int, int> pq = new PriorityQueue<int, int>();
        
        foreach (Order o in orders)
        {
            pq.Enqueue(o.Id, o.Priority == Priority.Expedited ? 0 : 1);
        }
        var orderedByPriority = new List<int>();

        while (pq.TryDequeue(out int id, out _))
        {
            orderedByPriority.Add(id);
        }
        return orderedByPriority;
    }
}
