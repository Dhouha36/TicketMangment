export interface ContratProjet {
  id:           number;
  dateDebut:    string;      // au format ISO yyyy-MM-dd
  dateFin?:     string;
  montantTotal?: number;
  projetId:     number;
}