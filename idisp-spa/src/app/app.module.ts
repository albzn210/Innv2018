import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { IdleComponent } from './idle/idle.component';
import { PaymentComponent } from './payment/payment.component';
import { ConfirmComponent } from './confirm/confirm.component';
import { TransactionComponent } from './transaction/transaction.component';
import { WebsocketService } from './websocket.service';
import { TerminalService } from './terminal.service';
import { appRoutes } from './routes';

@NgModule({
  declarations: [
    AppComponent,
    IdleComponent,
    PaymentComponent,
    ConfirmComponent,
    TransactionComponent
  ],
  imports: [
    BrowserModule,
    RouterModule.forRoot(appRoutes),
  ],
  providers: [
    WebsocketService,
    TerminalService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
