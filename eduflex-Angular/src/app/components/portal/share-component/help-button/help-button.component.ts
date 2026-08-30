import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HelpService } from '../../../../services/help.service';

@Component({
  selector: 'app-help-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './help-button.component.html',
  styleUrls: ['./help-button.component.css']
})
export class HelpButtonComponent {
  constructor(private helpService: HelpService) {}

  openHelp(): void {
    this.helpService.openCurrentTopic();
  }
}
