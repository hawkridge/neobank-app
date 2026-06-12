namespace NeoBank.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    
    private User() { }

    public User(string email, string passwordHash, string firstName, string lastName, string phoneNumber)
    {
        Id = Guid.NewGuid();
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void UpdateProfile(string firstName, string lastName, string? avatarUrl)
    {
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableTwoFactor(string encryptedSecret)
    {
        TwoFactorSecret = encryptedSecret;
        TwoFactorEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}