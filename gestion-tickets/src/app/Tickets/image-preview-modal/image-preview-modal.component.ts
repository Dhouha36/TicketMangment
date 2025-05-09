import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-image-preview-modal',
  imports: [],
  templateUrl: './image-preview-modal.component.html',
  styleUrl: './image-preview-modal.component.css'
})
export class ImagePreviewModalComponent {
  @Input() url!: string;
  // Cette méthode sera patchée depuis l'instance retournée par OverlayModalService
  close!: () => void;
}
