using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Factories;

namespace InternshipTracker.Tests;

public class UserCoreFactoryTests
{
    private readonly UserCoreFactory _factory = new();

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    [Test]
    public void Create_WithValidEmail_ReturnsUserCoreWithAllFieldsSet()
    {
        var id = Guid.NewGuid();
        const string name = "Alice";
        const string email = "alice@example.com";
        const CandidateLevel level = CandidateLevel.Junior;

        var user = _factory.Create(id, name, email, level);

        Assert.That(user.Id, Is.EqualTo(id));
        Assert.That(user.Name, Is.EqualTo(name));
        Assert.That(user.Email, Is.EqualTo(email));
        Assert.That(user.Level, Is.EqualTo(level));
    }

    [TestCase("user@domain.com")]
    [TestCase("user.name+tag@sub.domain.org")]
    [TestCase("USER@DOMAIN.COM")]
    [TestCase("u@d.io")]
    [TestCase("first.last@company.co.uk")]
    public void Create_WithVariousValidEmails_DoesNotThrow(string email)
    {
        Assert.DoesNotThrow(() => _factory.Create(Guid.NewGuid(), "Test", email, CandidateLevel.Junior));
    }

    // -------------------------------------------------------------------------
    // Empty / whitespace
    // -------------------------------------------------------------------------

    [Test]
    public void Create_WithEmptyEmail_ThrowsInvalidEmailException()
    {
        var ex = Assert.Throws<InvalidEmailException>(
            () => _factory.Create(Guid.NewGuid(), "Test", string.Empty, CandidateLevel.Junior));

        Assert.That(ex.ErrorCode, Is.EqualTo("User.InvalidEmail"));
    }

    [Test]
    public void Create_WithWhitespaceEmail_ThrowsInvalidEmailException()
    {
        Assert.Throws<InvalidEmailException>(
            () => _factory.Create(Guid.NewGuid(), "Test", "   ", CandidateLevel.Junior));
    }

    // -------------------------------------------------------------------------
    // Malformed addresses
    // -------------------------------------------------------------------------

    [TestCase("notanemail")]
    [TestCase("missing@tld")]
    [TestCase("@nodomain.com")]
    [TestCase("no-at-sign")]
    [TestCase("double@@domain.com")]
    [TestCase("space in@domain.com")]
    [TestCase("user@.com")]
    public void Create_WithMalformedEmail_ThrowsInvalidEmailException(string email)
    {
        var ex = Assert.Throws<InvalidEmailException>(
            () => _factory.Create(Guid.NewGuid(), "Test", email, CandidateLevel.Junior));

        Assert.That(ex.ErrorCode, Is.EqualTo("User.InvalidEmail"));
        Assert.That(ex.Message, Does.Contain(email));
    }

    // -------------------------------------------------------------------------
    // Length guard
    // -------------------------------------------------------------------------

    [Test]
    public void Create_WithEmailExceedingMaxLength_ThrowsInvalidEmailException()
    {
        // Build an email that exceeds 320 characters
        var local = new string('a', 300);
        var email = $"{local}@example.com"; // 315 chars — still under; push it over
        var overLimit = email.PadRight(321, 'x') + "@x.com"; // definitely > 320

        Assert.Throws<InvalidEmailException>(
            () => _factory.Create(Guid.NewGuid(), "Test", overLimit, CandidateLevel.Junior));
    }

    // -------------------------------------------------------------------------
    // ErrorCode & message shape
    // -------------------------------------------------------------------------

    [Test]
    public void Create_WithInvalidEmail_ExceptionMessageContainsOffendingEmail()
    {
        const string badEmail = "not-valid";

        var ex = Assert.Throws<InvalidEmailException>(
            () => _factory.Create(Guid.NewGuid(), "Test", badEmail, CandidateLevel.Junior));

        Assert.That(ex.Message, Does.Contain(badEmail));
        Assert.That(ex.ErrorCode, Is.EqualTo("User.InvalidEmail"));
    }

    [Test]
    public void Create_InvalidEmail_IsSubclassOfDomainException()
    {
        var ex = Assert.Throws<InvalidEmailException>(
            () => _factory.Create(Guid.NewGuid(), "Test", "bad", CandidateLevel.Junior));

        Assert.That(ex, Is.InstanceOf<CoreService.Domain.Exceptions.DomainException>());
    }

    // -------------------------------------------------------------------------
    // Name guard (entity constructor invariant)
    // -------------------------------------------------------------------------

    [Test]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => _factory.Create(Guid.NewGuid(), string.Empty, "valid@example.com", CandidateLevel.Junior));
    }

    [Test]
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => _factory.Create(Guid.NewGuid(), "   ", "valid@example.com", CandidateLevel.Junior));
    }

    // -------------------------------------------------------------------------
    // Level propagation
    // -------------------------------------------------------------------------

    [TestCase(CandidateLevel.Trainee)]
    [TestCase(CandidateLevel.Junior)]
    [TestCase(CandidateLevel.Middle)]
    [TestCase(CandidateLevel.Senior)]
    public void Create_AllCandidateLevels_ArePreservedOnEntity(CandidateLevel level)
    {
        var user = _factory.Create(Guid.NewGuid(), "Test", "test@example.com", level);

        Assert.That(user.Level, Is.EqualTo(level));
    }
}

