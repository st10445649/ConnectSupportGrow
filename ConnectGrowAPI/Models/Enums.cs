namespace ConnectGrowAPI.Models;

public enum WebinarStatus
{
    Draft = 0,
    Published = 1,
    Ongoing = 2,
    Completed = 3,
    Cancelled = 4
}

//booking lifecycle
public enum BookingStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
    Attended = 3,
    NoShow = 4
}


// Outcome of a single payment attempt. 
//Every attempt is recorded, including failures.
//therefore the transaction table doubles as a payment audit trail
public enum TransactionStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Refunded = 3
}

public enum BlogPostStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}