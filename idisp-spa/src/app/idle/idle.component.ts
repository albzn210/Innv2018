import { Component, OnInit, Input } from '@angular/core';
import {TerminalService} from '../terminal.service';
import { TerminalCommand } from '../terminal.service.messages';

@Component({
  selector: 'app-idle',
  templateUrl: './idle.component.html',
  styleUrls: ['./idle.component.css']
})
export class IdleComponent implements OnInit {

  @Input() state: string;

  constructor(private terminalService: TerminalService) {
  }

  ngOnInit() {
  }

  class() {
    if (this.state === 'idle') {
      return 'rfid';
    } else if (this.state === 'transaction') {
      return 'rfid success';
    } else {
      return '';
    }
  }

  confirm() {
    this.terminalService.send(new TerminalCommand('ConfirmAll'));
  }
}
