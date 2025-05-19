import { Pays } from "./pays";
import { Projet } from "./Projet";
import { User } from "./user";

export interface Societe {
  id: number;
  nom: string;
  adresse: string;
  telephone: string;
  paysId: number;
  ville: string;
  codePostal: string;
  selected?: boolean;
  utilisateurs: User[];
  projets: Projet[]; 
  pays?: Pays; 
}
