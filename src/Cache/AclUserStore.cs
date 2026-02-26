namespace codecrafters_redis.src.Cache;

using System.Security.Cryptography;
using System.Text;

public sealed record AclUser(string Name, IReadOnlyList<string> Passwords)
{
  public bool HasNoPassword => Passwords.Count == 0;
}

public interface IAclUserStore
{
  bool TryGetUser(string username, out AclUser user);
  bool VerifyPassword(string username, string password);
  void AddPassword(string username, string password);
}

public sealed class AclUserStore : IAclUserStore
{
  private static readonly AclUser EmptyUser = new(string.Empty, []);
  private readonly Dictionary<string, List<string>> _users = new(StringComparer.Ordinal)
  {
    ["default"] = [],
  };

  public bool TryGetUser(string username, out AclUser user)
  {
    if (_users.TryGetValue(username, out List<string>? passwords))
    {
      user = new(username, [.. passwords]);
      return true;
    }

    user = EmptyUser;
    return false;
  }

  public bool VerifyPassword(string username, string password)
  {
    if (!_users.TryGetValue(username, out List<string>? passwords))
    {
      return false;
    }

    if (passwords.Count == 0)
    {
      return true;
    }

    string hashedPassword = HashPassword(password);
    return passwords.Contains(hashedPassword, StringComparer.Ordinal);
  }

  public void AddPassword(string username, string password)
  {
    string hashedPassword = HashPassword(password);

    if (!_users.TryGetValue(username, out List<string>? passwords))
    {
      passwords = [];
      _users[username] = passwords;
    }

    if (!passwords.Contains(hashedPassword, StringComparer.Ordinal))
    {
      passwords.Add(hashedPassword);
    }
  }

  private static string HashPassword(string password)
  {
    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
    byte[] hashBytes = SHA256.HashData(passwordBytes);
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
  }
}
