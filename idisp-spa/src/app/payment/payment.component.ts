import { Component } from '@angular/core';
import { TerminalService } from '../terminal.service';
import { Payment } from '../terminal.service.interfaces';
import { TerminalCommand } from '../terminal.service.messages';

@Component({
  selector: 'app-payment',
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css']
})
export class PaymentComponent {

  constructor(private terminalService: TerminalService) {
    terminalService.getMessage().subscribe(
      msg => {
        if (msg.Command === 'Payment') {
          console.log('On Payment');
          const payment = msg.Args as Payment;
          console.log(payment.Price);
        }
      });
  }

  confirm() {
    this.terminalService.send(new TerminalCommand('ConfirmPayment'));
  }
}
