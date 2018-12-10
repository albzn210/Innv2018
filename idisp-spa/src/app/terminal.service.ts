import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, Subject, throwError } from 'rxjs';
import { map, catchError, retry } from 'rxjs/operators';
import { WebsocketService } from './websocket.service';
import { Message } from './terminal.service.interfaces';
import { TerminalCommand } from './terminal.service.messages';

const CHAT_URL = 'ws://localhost:8080/service';

@Injectable()
export class TerminalService {
  public messages: Subject<any>;
  private relay = new Subject<Message>();

  constructor(wsService: WebsocketService, private router: Router) {
    this.messages = <Subject<any>>wsService
      .connect(CHAT_URL).pipe (
        map((response: MessageEvent): Message => {
          const data = JSON.parse(response.data);
          console.log(data);
          this.relay.next(data);
          return {
            Command: data.Command,
            Args: data.Args
          };
        }),
        catchError((e: Response) => throwError(e))
      );
  }

  getMessage(): Observable<Message> {
    return this.relay.asObservable();
  }

  send(msg: TerminalCommand) {
    console.log(msg);
    this.messages.next(msg);
  }
}
