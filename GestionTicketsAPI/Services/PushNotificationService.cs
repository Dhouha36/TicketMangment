// PushNotificationService.cs
using GestionTicketsAPI.Controllers;
using GestionTicketsAPI.Interfaces;
using Newtonsoft.Json;
using WebPush;

public class PushNotificationService : IPushNotificationService
{
  private readonly ISubscriptionRepository _subscriptionRepository; // Implémentez ce repository pour stocker/récupérer les abonnements
  private readonly VapidDetails _vapidDetails;
  private readonly WebPushClient _webPushClient;

  public PushNotificationService(ISubscriptionRepository subscriptionRepository)
  {
    _subscriptionRepository = subscriptionRepository;
    // Assurez-vous d'unifier les clés et l'email (ici "contact@votredomaine.com")
    _vapidDetails = new VapidDetails(
        "mailto:contact@votredomaine.com",
        "BEEZtKVh995Du3gHeCl_b80WUS6-b3D6gfqEj5IdKJTNYKkeERqBKgWbi_3I9ObOCdpdG7zr1KkUjHGiePRX0GM",
        "88oPmDyL8iU8yOvBthBNOcs4PsDU6FRc31oE0V93kos"
    );
    _webPushClient = new WebPushClient();
  }

  public async Task SendNotificationToAllAsync(NotificationPayload payload)
  {
    var subscriptions = await _subscriptionRepository.GetAllSubscriptionsAsync();
    string jsonPayload = JsonConvert.SerializeObject(payload);

    foreach (var sub in subscriptions)
    {
      // Conversion de l'entité en WebPush.PushSubscription
      var webPushSubscription = new WebPush.PushSubscription(
          sub.Endpoint,
          sub.P256DH,
          sub.Auth
      );

      try
      {
        // Préparer les options pour VAPID
        var vapidOptions = new Dictionary<string, object>
            {
                { "vapidDetails", _vapidDetails }
            };

        await _webPushClient.SendNotificationAsync(webPushSubscription, jsonPayload, vapidOptions);
      }
      catch (Exception ex)
      {
        // Logger l'exception et/ou gérer l'abonnement invalide
        Console.WriteLine($"Erreur lors de l'envoi de la notification push : {ex.Message}");
      }
    }
  }

}
