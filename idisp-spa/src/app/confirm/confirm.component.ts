import { Component, OnInit } from '@angular/core';
import { TerminalService } from '../terminal.service';
import { TerminalCommand } from '../terminal.service.messages';

@Component({
  selector: 'app-confirm',
  templateUrl: './confirm.component.html',
  styleUrls: ['./confirm.component.css']
})
export class ConfirmComponent implements OnInit {

  constructor(private terminalService: TerminalService) {}

  ngOnInit() {
  }

  confirm() {
    this.terminalService.send(new TerminalCommand('ConfirmAll'));
  }
}
