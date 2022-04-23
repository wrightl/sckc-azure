using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace admin.app.Extensions
{
    public static class WebHostEnvironmentExtensions
	{
        public static string MapPath(this IWebHostEnvironment context, string path)
        {
			if (path == null) return null;
			if (path.StartsWith("/"))
			{
				path = path.Substring(1);
			}
			string text = path;
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			path = text.Replace("/", directorySeparatorChar.ToString());
			string physicalApplicationPath = context.WebRootPath;
			return Path.Combine(physicalApplicationPath, path);
		}
    }
}
