import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { TerminalService } from './terminal.service';
import { WebsocketService } from './websocket.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  constructor(private terminalSerivce: TerminalService, private router: Router) {
    terminalSerivce.messages.subscribe(
      msg => {
        if (msg.Command === 'View') {
          const route = '/' + msg.Args as string;
          console.log('Go to ' + route);
          this.router.navigate(['/' + route]);
        }
      });
  }
}
