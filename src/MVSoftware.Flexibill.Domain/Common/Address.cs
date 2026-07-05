namespace MVSoftware.Flexibill.Domain.Common;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string HouseNumber { get; }
    public string PostalCode { get; }
    public string City { get; }
    public string Country { get; }

    private Address(string street, string houseNumber, string postalCode, string city, string country)
    {
        Street = street;
        HouseNumber = houseNumber;
        PostalCode = postalCode;
        City = city;
        Country = country;
    }

    public static Address Of(string street, string houseNumber, string postalCode, string city, string country = "NL")
    {
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.", nameof(city));

        return new Address(street, houseNumber, postalCode, city, country);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return HouseNumber;
        yield return PostalCode;
        yield return City;
        yield return Country;
    }

    public override string ToString() => $"{Street} {HouseNumber}, {PostalCode} {City}, {Country}";
}
