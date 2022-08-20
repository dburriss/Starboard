using System.Runtime.CompilerServices;

namespace Starboard.Dtos
{
    public class Helper
    {
        public static string GetThisFilePath([CallerFilePath] string? path = null)
        {
            return path;
        }
    }
}
