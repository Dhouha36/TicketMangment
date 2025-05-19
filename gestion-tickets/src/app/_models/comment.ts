import { ClientDto } from "../DTOs/ClientDto";
import { Photo } from "./photo";
import { User } from "./user";

export interface Comment {
  id: number;
  contenu?: string;
  date: Date;
  utilisateurId: number;
  utilisateur?: User;
  clientId?: number;
  client?: ClientDto; 
  ticketId: number;
  photos?: Photo[];
}
