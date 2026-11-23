namespace RailWeldApp.Interfaces;

public interface IPasswordService
{
    string CreateSalt();

    string HashPassword(string password, string salt);

    bool VerifyPassword(string password, string passwordHash, string passwordSalt);
}
