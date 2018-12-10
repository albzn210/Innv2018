import { Component, OnInit } from '@angular/core';
import {TerminalService} from '../terminal.service';

@Component({
  selector: 'app-idle',
  templateUrl: './idle.component.html',
  styleUrls: ['./idle.component.css']
})
export class IdleComponent implements OnInit {

  /*constructor(private terminalService: TerminalService) {
    terminalService.messages.subscribe(msg => {
      switch (msg.command) {
        case 'sleep': {
          break;
        }
      }
    });
  }*/

  ngOnInit() {
  }
}
