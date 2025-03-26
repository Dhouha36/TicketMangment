using System;
using System.Collections.Generic;

namespace GestionTicketsAPI.Helpers
{
    public static class PasswordResetStore
    {
        // Clé : Email, Valeur : (Token, Expiration)
        public static Dictionary<string, (string Token, DateTime Expiration)> ResetTokens { get; } 
            = new Dictionary<string, (string, DateTime)>();
    }
}
