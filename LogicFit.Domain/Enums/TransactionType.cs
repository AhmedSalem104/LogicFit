namespace LogicFit.Domain.Enums;

public enum TransactionType
{
    Deposit = 0,      // إيداع
    Withdrawal = 1,   // سحب
    Payment = 2,      // دفع (اشتراك مثلاً)
    Refund = 3,       // استرداد
    Adjustment = 4    // تعديل يدوي
}
