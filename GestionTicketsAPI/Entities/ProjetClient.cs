using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTicketsAPI.Entities
{
    public class ProjetClient
    {
        // Clés composites
        public int ProjetId  { get; set; }
        public int ClientId  { get; set; }

        // Navigations
        public Projet Projet { get; set; } = null!;
        public Client Client { get; set; } = null!;
    }
}
