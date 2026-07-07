using LogicFit.Application.Common.Notifications;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public class NotificationTemplatesTests
{
    [Fact]
    public void Approved_Template_Has_Activation_Title()
    {
        var r = NotificationTemplates.Render(NotificationTemplates.PaymentRequestApproved);
        Assert.False(string.IsNullOrWhiteSpace(r.Title));
        Assert.Equal(NotificationType.General, r.Type);
    }

    [Fact]
    public void Rejected_Template_Includes_Reason()
    {
        var r = NotificationTemplates.Render(
            NotificationTemplates.PaymentRequestRejected,
            new Dictionary<string, string> { ["reason"] = "الصورة غير واضحة" });

        Assert.Contains("الصورة غير واضحة", r.Body);
    }

    [Fact]
    public void ExpiringSoon_Template_Uses_SubscriptionExpiring_Type()
    {
        var r = NotificationTemplates.Render(
            NotificationTemplates.SubscriptionExpiringSoon,
            new Dictionary<string, string> { ["days"] = "3" });

        Assert.Equal(NotificationType.SubscriptionExpiring, r.Type);
        Assert.Contains("3", r.Body);
    }

    [Fact]
    public void Unknown_Template_Falls_Back_Gracefully()
    {
        var r = NotificationTemplates.Render("does-not-exist");
        Assert.Equal(NotificationType.General, r.Type);
    }
}
