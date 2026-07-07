using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Notifications;

/// <summary>Renders subscription/billing notification templates (bilingual-friendly, Arabic body).</summary>
public static class NotificationTemplates
{
    public const string PaymentRequestApproved = "PaymentRequestApproved";
    public const string PaymentRequestRejected = "PaymentRequestRejected";
    public const string SubscriptionExpiringSoon = "SubscriptionExpiringSoon";
    public const string SubscriptionExpired = "SubscriptionExpired";
    public const string TenantSuspended = "TenantSuspended";

    public static RenderedNotification Render(string code, IReadOnlyDictionary<string, string>? data = null)
    {
        string Get(string key, string fallback = "") =>
            data != null && data.TryGetValue(key, out var v) ? v : fallback;

        return code switch
        {
            PaymentRequestApproved => new("تم تفعيل اشتراكك",
                "تم تفعيل اشتراكك بنجاح. شكراً لك.", NotificationType.General),

            PaymentRequestRejected => new("تم رفض إثبات الدفع",
                $"تم رفض إثبات الدفع بسبب: {Get("reason", "غير محدد")}. برجاء إعادة رفع إثبات صحيح.",
                NotificationType.General),

            SubscriptionExpiringSoon => new("اشتراكك على وشك الانتهاء",
                $"اشتراكك سينتهي خلال {Get("days", "بضعة")} أيام. برجاء التجديد لتجنب إيقاف الخدمة.",
                NotificationType.SubscriptionExpiring),

            SubscriptionExpired => new("انتهى اشتراكك",
                "انتهى اشتراكك. برجاء التجديد لاستعادة الخدمة.", NotificationType.SubscriptionExpiring),

            TenantSuspended => new("تم إيقاف الاشتراك",
                "تم إيقاف اشتراك الجيم لعدم التجديد. برجاء التواصل أو التجديد لإعادة التفعيل.",
                NotificationType.General),

            _ => new("إشعار", Get("body", ""), NotificationType.General)
        };
    }
}

public record RenderedNotification(string Title, string Body, NotificationType Type);
