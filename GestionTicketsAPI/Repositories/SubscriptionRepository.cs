using System;
using GestionTicketsAPI.Data;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionTicketsAPI.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
  private readonly DataContext _context;

  public SubscriptionRepository(DataContext context)
  {
    _context = context;
  }

  public async Task AddSubscriptionAsync(PushSubscription subscription)
  {
    _context.PushSubscriptions.Add(subscription);
    await _context.SaveChangesAsync();
  }

  public async Task<IEnumerable<PushSubscription>> GetAllSubscriptionsAsync()
  {
    return await _context.PushSubscriptions.ToListAsync();
  }


  public async Task DeleteSubscriptionAsync(int id)
  {
    var subscription = await _context.PushSubscriptions.FindAsync(id);
    if (subscription != null)
    {
      _context.PushSubscriptions.Remove(subscription);
      await _context.SaveChangesAsync();
    }
  }
}

