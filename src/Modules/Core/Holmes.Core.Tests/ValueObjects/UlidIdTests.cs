using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Core.Tests.ValueObjects;

[TestFixture]
public class UlidIdTests
{
    [Test]
    public void Parse_Roundtrips_String()
    {
        var original = UlidId.NewUlid();
        var parsed = UlidId.Parse(original.ToString());

        Assert.That(parsed, Is.EqualTo(original));
    }

    [Test]
    public void TryParse_Invalid_Returns_False()
    {
        var success = UlidId.TryParse("not-a-ulid", out _);
        Assert.That(success, Is.False);
    }
}
