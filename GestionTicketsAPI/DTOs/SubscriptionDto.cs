using System;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.DTOs;

public class SubscriptionDto
{
    public string Endpoint { get; set; }
    public SubscriptionKeys Keys { get; set; }
}