
namespace GestionTicketsAPI.DTOs;

public enum TypeContrat
{
    CDD,
    CDI,
}
public class ContratUserDto
{
  public int Id { get; set; }
  public DateTime DateDebut { get; set; }
  public DateTime? DateFin { get; set; }
  public TypeContrat Type { get; set; }
  public decimal? SalaireMensuel { get; set; }
  public int UserId { get; set; }
}

