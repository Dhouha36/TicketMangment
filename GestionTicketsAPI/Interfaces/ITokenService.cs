using System;
using GestionTicketsAPI.Entities;

namespace GestionTicketsAPI.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
    string CreateToken(Client client);
}
