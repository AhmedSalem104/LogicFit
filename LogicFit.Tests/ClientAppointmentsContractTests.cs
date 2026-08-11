using LogicFit.Application.Features.ClientDashboard.Queries.GetMyAppointments;
using LogicFit.Domain.Enums;
using Xunit;

namespace LogicFit.Tests;

public sealed class ClientAppointmentsContractTests
{
    [Fact]
    public void Client_appointment_status_uses_the_shared_enum_contract()
    {
        var dto = new MyAppointmentDto { Status = AppointmentStatus.Confirmed };

        Assert.Equal(AppointmentStatus.Confirmed, dto.Status);
        Assert.Equal(typeof(AppointmentStatus), typeof(MyAppointmentDto).GetProperty(nameof(MyAppointmentDto.Status))!.PropertyType);
    }

    [Fact]
    public void Client_appointment_query_projects_the_enum_without_string_conversion()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var handler = File.ReadAllText(Path.Combine(root, "LogicFit.Application", "Features", "ClientDashboard", "Queries", "GetMyAppointments", "GetMyAppointmentsQueryHandler.cs"));

        Assert.Contains("Where(a => a.TenantId == tenantId && a.ClientId == userId)", handler, StringComparison.Ordinal);
        Assert.Contains("Status = a.Status", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("Status = a.Status.ToString()", handler, StringComparison.Ordinal);
    }
}
