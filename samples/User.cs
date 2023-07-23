namespace DtosDemo;
using System.Domain;
using System.Net.Mail;
using System.Net.Mime;

[GenerateDto(DtoType.Dto, DataStructureType.RecordStruct)]
public class User
{
    public const string DefaultPassword = "Just1n is really fuckin sexy!";
    public const string DefaultLockoutEndString = "1/1/1970";

    /// <summary>The user's ID unique to The Dgmjr family of apps and bots.</summary>
    public virtual int Id { get; set; }
    public virtual string? TelegramUsername { get; set; }
    /// <summary>Gets or sets the user's Telegram ID 64, a-bit signed integer</summary>
    /// <example>1234567</example>
    public virtual long TelegramId { get; set; }

    public virtual string? GivenName { get; set; } = null;
    public virtual string? Surname { get; set; } = null;
    public virtual string? GoByName { get; set; } = null;

    /// <summary>The number of times the user tried and failed to authenticate.</summary>
    public virtual int AccessFailedCount { get; set; } = 0;
    /// <summary>Gets or sets a flag indicating if the user can be locked out.</summary>
    /// <value><pre><b>True</b></pre> if the user <b><b>can</b></b> be locked out, <pre><b>false</b></pre> otherwise.</value>
    public virtual bool IsLockoutEnabled { get; set; } = true;
    /// <summary>Gets or sets a flag indicating if a user has confirmed his telephone number.</summary>
    /// <value><pre><b>True</b></pre> if the user has confirmed ownership of the <see cref="Email"/> address in his profile, <pre><b>false</b></pre> otherwise.</value>
    public virtual bool IsEmailConfirmed { get; set; }
    /// <summary>Gets or sets a flag indicating if a user has confirmed his telephone address.</summary>
    /// <value><pre><b>True</b></pre> if the user has confirmed ownership of the <see cref="PhoneNumber"/> in his profile, <pre><b>false</b></pre> otherwise.</value>
    public virtual bool IsPhoneNumberConfirmed { get; set; }
    /// <summary>Gets or sets a flag indicating if a user has two-factor authentication set up.</summary>
    /// <value><pre><b>True</b></pre> if the user has two-factor authentication set up,  <pre><b>false</b></pre> otherwise.</value>
    public virtual bool IsTwoFactorEnabled { get; set; }
    /// <summary>Gets or sets a flag indicating whether the user has been locked out (either deliberately be an administrator or by exhausting the number of attempts allowed to authenticate.</summary>
    /// <value><pre>True</pre> if the user <b><i>is locked out</i></b> right now, <pre><b>false</b></pre> otherwise.</value>
    public virtual bool IsLockedOut => IsLockoutEnabled && LockoutEnd > DateTimeOffset.UtcNow;

    public virtual DateTimeOffset? LockoutEnd { get; set; } = DateTimeOffset.Parse(DefaultLockoutEndString);

    /// <summary>Gets or sets the user's email address as a string.</summary>
    public virtual string? Email { get; set; }
    /// <summary>Gets or sets the normalized email address for this user as a string.</summary>
    public virtual string? NormalizedEmail { get; set; }
    /// <summary>Gets or sets the user's username (usually the same as the <see cref="TelegramUsername" />)</summary>
    /// <example>Dgmjr</example>
    public virtual string? UserName { get; set; }

    public virtual string? NormalizedUserName { get; set; }
    public override bool Equals(object? obj)
        => obj is User user && user.Id == Id;

    public override int GetHashCode() => Id;

    /// <example>+19185256012</example>
    public virtual string? PhoneNumber { get; set; }

    public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>A hashed and salted representation of the user's password.</summary>
    public virtual string? PasswordHash { get; set; } = null;
    /// <summary>A random value that must change whenever a users credentials change (password changed, login removed)</summary>
    public virtual string? SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Gets or sets the normalized email address for this user as a data structure.</summary>
    public virtual EmailAddress? NormalizedEmailAddress { get => NormalizedEmail; set => NormalizedEmail = value; }
    /// <summary>Gets or sets the the user's email address as a data structure.</summary>
    public virtual EmailAddress? EmailAddress { get => Email; set => Email = value; }
    public virtual PhoneNumber? Phone { get => PhoneNumber; set => PhoneNumber = value; }
}
