using System;

namespace GestionTicketsAPI.DTOs;

public class PushSubscriptionDto
{
  public string UserId { get; set; } = null!;

  public SubscriptionInfo Subscription { get; set; } = null!;
}

public class SubscriptionInfo
{
  public string Endpoint { get; set; } = null!;
  public SubscriptionKeys Keys { get; set; } = null!;
}

public class SubscriptionKeys
{
  public string P256dh { get; set; } = null!;
  public string Auth { get; set; } = null!;
}
