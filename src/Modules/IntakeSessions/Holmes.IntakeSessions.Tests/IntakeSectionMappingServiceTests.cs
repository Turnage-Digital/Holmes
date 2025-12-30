using Holmes.IntakeSessions.Contracts.Services;
using Holmes.IntakeSessions.Application.Services;

namespace Holmes.IntakeSessions.Tests;

public class IntakeSectionMappingServiceTests
{
    private IIntakeSectionMappingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new IntakeSectionMappingService();
    }

    [Test]
    public void GetRequiredSections_EmptyList_ReturnsEmpty()
    {
        var result = _sut.GetRequiredSections([]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetRequiredSections_UnknownServiceCode_ReturnsEmpty()
    {
        var result = _sut.GetRequiredSections(["UNKNOWN_CODE"]);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetRequiredSections_EmploymentService_ReturnsEmploymentSection()
    {
        var result = _sut.GetRequiredSections(["TWN_EMP"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Employment));
    }

    [Test]
    public void GetRequiredSections_MultipleEmploymentServices_ReturnsEmploymentOnce()
    {
        var result = _sut.GetRequiredSections(["TWN_EMP", "DIRECT_EMP", "INCOME_VERIFY"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Employment));
    }

    [Test]
    public void GetRequiredSections_EducationServices_ReturnsEducationSection()
    {
        var result = _sut.GetRequiredSections(["EDU_VERIFY"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Education));
    }

    [Test]
    public void GetRequiredSections_ProfessionalLicense_ReturnsEducationSection()
    {
        var result = _sut.GetRequiredSections(["PROF_LICENSE"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Education));
    }

    [Test]
    public void GetRequiredSections_ReferenceService_ReturnsReferencesSection()
    {
        var result = _sut.GetRequiredSections(["PROF_REF"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.References));
    }

    [Test]
    public void GetRequiredSections_DrugTest_ReturnsPhoneSection()
    {
        var result = _sut.GetRequiredSections(["DRUG_TEST"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Phone));
    }

    [Test]
    public void GetRequiredSections_MvrService_ReturnsDrivingInfoSection()
    {
        var result = _sut.GetRequiredSections(["MVR"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.DrivingInfo));
    }

    [Test]
    public void GetRequiredSections_CdlVerify_ReturnsDrivingInfoSection()
    {
        var result = _sut.GetRequiredSections(["CDL_VERIFY"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.DrivingInfo));
    }

    [Test]
    public void GetRequiredSections_MixedServices_ReturnsAllApplicableSections()
    {
        var result = _sut.GetRequiredSections(["TWN_EMP", "EDU_VERIFY", "PROF_REF", "DRUG_TEST"]);

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result, Contains.Item(IntakeSections.Employment));
        Assert.That(result, Contains.Item(IntakeSections.Education));
        Assert.That(result, Contains.Item(IntakeSections.References));
        Assert.That(result, Contains.Item(IntakeSections.Phone));
    }

    [Test]
    public void GetRequiredSections_CaseInsensitive_ReturnsCorrectSection()
    {
        var result = _sut.GetRequiredSections(["twn_emp", "TWN_EMP", "Twn_Emp"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Employment));
    }

    [Test]
    public void GetRequiredSections_MixedKnownAndUnknown_ReturnsOnlyKnownSections()
    {
        var result = _sut.GetRequiredSections(["TWN_EMP", "UNKNOWN_CODE", "COUNTY_CRIM"]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Contains.Item(IntakeSections.Employment));
    }
}