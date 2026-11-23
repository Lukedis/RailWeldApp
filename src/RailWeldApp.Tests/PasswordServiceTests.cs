using Microsoft.VisualStudio.TestTools.UnitTesting;
using RailWeldApp.Services;

namespace RailWeldApp.Tests;

[TestClass]
public sealed class PasswordServiceTests
{
    [TestMethod]
    public void HashAndVerify_ReturnsTrueForCorrectPassword()
    {
        var service = new PasswordService();
        string salt = service.CreateSalt();
        string hash = service.HashPassword("admin123", salt);

        Assert.IsTrue(service.VerifyPassword("admin123", hash, salt));
        Assert.IsFalse(service.VerifyPassword("wrong", hash, salt));
    }

    [TestMethod]
    public void SamePassword_DifferentSalt_ProducesDifferentHash()
    {
        var service = new PasswordService();
        string hash1 = service.HashPassword("admin123", service.CreateSalt());
        string hash2 = service.HashPassword("admin123", service.CreateSalt());

        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void EmptyInput_ReturnsFalse()
    {
        var service = new PasswordService();

        Assert.IsFalse(service.VerifyPassword(string.Empty, "hash", "salt"));
        Assert.IsFalse(service.VerifyPassword("password", string.Empty, "salt"));
        Assert.IsFalse(service.VerifyPassword("password", "hash", string.Empty));
    }
}
