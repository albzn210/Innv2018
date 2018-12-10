import { Component } from '@angular/core';
import { TerminalService } from '../terminal.service';
import { Transaction } from '../terminal.service.interfaces';
import { TerminalCommand } from '../terminal.service.messages';

@Component({
  selector: 'app-transaction',
  templateUrl: './transaction.component.html',
  styleUrls: ['./transaction.component.css']
})
export class TransactionComponent {

  transaction: Transaction;

  constructor(private terminalService: TerminalService) {
    terminalService.getMessage().subscribe(
      msg => {
        if (msg.Command === 'Transaction') {
          console.log('On Transaction');
          this.transaction = msg.Args as Transaction;
          console.log(this.transaction.LP);
        }
      });
  }

  confirm() {
    this.terminalService.send(new TerminalCommand('ConfirmTransaction'));
  }
}
