using GestionTicketsAPI.DTOs;
using GestionTicketsAPI.Entities;
using GestionTicketsAPI.Interfaces; // Assurez-vous d'importer le namespace de l'interface
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GestionTicketsAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class PushController : ControllerBase
  {
    private readonly ISubscriptionRepository _subscriptionRepository;

    // Injection du repository via le constructeur
    public PushController(ISubscriptionRepository subscriptionRepository)
    {
      _subscriptionRepository = subscriptionRepository;
    }

    // Vous pouvez sauvegarder les abonnements dans une base de données.
    // Pour simplifier, nous utilisons une liste statique (à ne pas utiliser en prod).
    private static readonly List<string> subscriptions = new List<string>();

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscriptionDto subscription)
    {
      if (subscription == null || subscription.Keys == null ||
          string.IsNullOrEmpty(subscription.Endpoint) ||
          string.IsNullOrEmpty(subscription.Keys.P256dh) ||
          string.IsNullOrEmpty(subscription.Keys.Auth))
      {
        return BadRequest("La structure de l'abonnement est invalide.");
      }

      var pushSubscription = new PushSubscription
      {
        Endpoint = subscription.Endpoint,
        P256DH = subscription.Keys.P256dh,
        Auth = subscription.Keys.Auth
      };

      await _subscriptionRepository.AddSubscriptionAsync(pushSubscription);
      return Ok();
    }



    // Méthode de test pour envoyer une notification à tous les abonnés
    [HttpPost("sendNotification")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationPayload payload)
    {
      // Parcourir les abonnements et envoyer la notification via WebPush
      foreach (var subJson in subscriptions)
      {
        // Convertir le JSON en WebPush.PushSubscription
        var subscription = Newtonsoft.Json.JsonConvert.DeserializeObject<WebPush.PushSubscription>(subJson);
        await SendPushNotification(subscription, payload);
      }
      return Ok();
    }

    private async Task SendPushNotification(WebPush.PushSubscription subscription, NotificationPayload payload)
    {
      // Configurez ici vos clés VAPID
      var vapidDetails = new WebPush.VapidDetails(
          "mailto:contact@votre-domaine.com",
          "BEEZtKVh995Du3gHeCl_b80WUS6-b3D6gfqEj5IdKJTNYKkeERqBKgWbi_3I9ObOCdpdG7zr1KkUjHGiePRX0GM", // Votre clé publique VAPID
          "88oPmDyL8iU8yOvBthBNOcs4PsDU6FRc31oE0V93kos"     // Votre clé privée VAPID
      );

      var webPushClient = new WebPush.WebPushClient();
      try
      {
        await webPushClient.SendNotificationAsync(subscription, Newtonsoft.Json.JsonConvert.SerializeObject(payload), vapidDetails);
      }
      catch (Exception ex)
      {
        // Loguer ou gérer l'exception
        Console.WriteLine($"Erreur lors de l'envoi d'une notification push : {ex.Message}");
      }
    }
  }

  // Classe pour le payload de la notification
  public class NotificationPayload
  {
    public string Title { get; set; }
    public string Body { get; set; }
    public string Icon { get; set; }
  }
}
