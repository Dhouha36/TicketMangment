export interface ContratUser {
  id:           number;
  dateDebut:    string;      // au format ISO yyyy-MM-dd
  dateFin?:     string;
  type:         'CDD' | 'CDI';
  salaireMensuel?: number;
  userId:       number;
}