using MVSoftware.Flexibill.Domain.Common;

namespace MVSoftware.Flexibill.Domain.Suppliers;

public sealed class ContactPerson : ValueObject
{
    public string Name { get; }
    public EmailAddress? Email { get; }
    public string? Phone { get; }

    private ContactPerson(string name, EmailAddress? email, string? phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
    }

    public static ContactPerson Of(string name, EmailAddress? email = null, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A contact person requires a name.", nameof(name));
        }

        return new ContactPerson(name, email, phone);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Email;
        yield return Phone;
    }
}
