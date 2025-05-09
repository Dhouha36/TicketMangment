import { Photo } from "./photo";
import { User } from "./user";

export interface Comment {
  id: number;
  contenu?: string;
  date: Date;
  utilisateurId: number;
  utilisateur?: User;
  ticketId: number;
  photos?: Photo[];
}
