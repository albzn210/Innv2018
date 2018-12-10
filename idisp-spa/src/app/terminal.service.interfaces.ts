export interface Message {
    Command: string;
    Args: any;
}

export interface  Transaction {
    TransactionID: string;
    LP: string;
    Time: Date;
    Price: Number;
}

export interface Payment {
    Price: Number;
}
