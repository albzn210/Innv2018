import { NgModule } from '@angular/core';
import { RouterModule, Routes} from '@angular/router';
import { IdleComponent } from './idle/idle.component';
import { TransactionComponent } from './transaction/transaction.component';
import { PaymentComponent } from './payment/payment.component';
import { ConfirmComponent } from './confirm/confirm.component';

export const appRoutes: Routes = [
    {path: '', component: IdleComponent},
    {
        path: '',
        runGuardsAndResolvers: 'always',
        children: [
            { path: 'idle', component: IdleComponent },
            { path: 'transaction', component: TransactionComponent },
            { path: 'payment', component: PaymentComponent },
            { path: 'confirm', component: ConfirmComponent },
        ]
    },
    {path: '**', redirectTo: '', pathMatch: 'full'},
];
