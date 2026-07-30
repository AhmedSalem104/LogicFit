using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Verified WebAuthn public credential. No private key or biometric material is ever stored by LogicFit.</summary>
public sealed class IdentityPasskeyCredential : AuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();
    public uint SignatureCounter { get; set; }
    public string? FriendlyName { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public IdentityAccount IdentityAccount { get; set; } = null!;
}
