using ClassSchedulingSys.Models;

namespace ClassSchedulingSys.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(ApplicationUser user, IList<string> roles);
    }
}
