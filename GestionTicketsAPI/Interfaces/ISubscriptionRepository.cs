using System;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Interfaces;

public interface ISubscriptionRepository
{
    Task AddSubscriptionAsync(PushSubscription subscription);
    Task<IEnumerable<PushSubscription>> GetAllSubscriptionsAsync();
    Task DeleteSubscriptionAsync(int id);
}

