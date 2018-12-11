import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { TerminalService } from './terminal.service';
import { WebsocketService } from './websocket.service';
import { TerminalCommand } from './terminal.service.messages';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  state: string;

  constructor(private terminalService: TerminalService, private router: Router) {
    terminalService.messages.subscribe(
      msg => {
        if (msg.Command === 'View') {
          this.state = msg.Args as string;
          console.log(this.state);
          /*const route = '/' + msg.Args as string;
          console.log('Go to ' + route);
          this.router.navigate(['/' + route]);*/
        }
      });
  }
}
