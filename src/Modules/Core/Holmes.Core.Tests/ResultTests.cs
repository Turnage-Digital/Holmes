using Holmes.Core.Application;

namespace Holmes.Core.Tests;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Success_Result_Has_Value()
    {
        var result = Result.Success(42);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void Fail_Result_Throws_On_Value()
    {
        var result = Result.Fail<int>("nope");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("nope"));
            Assert.That(() => _ = result.Value, Throws.InvalidOperationException);
        });
    }
}