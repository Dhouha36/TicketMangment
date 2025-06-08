import { ContratProjet } from "./contrat-projet";
import { User } from "./user";

export interface Projet {
  id: number;
  nom: string;
  description?: string;
  // L’un ou l’autre doit être renseigné
  societeId?: number | null;
  clientId?: number | null;
  chefProjetId?: number;
  chefProjet?: User;
  idPays: number;
  nomSociete?: string;  
  nomPays?: string;    
  selected?: boolean; 
  utilisateurs?: User[];
  contratProjet?: ContratProjet;
}

