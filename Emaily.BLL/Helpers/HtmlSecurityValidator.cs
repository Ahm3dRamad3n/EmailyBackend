using Ganss.Xss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers
{
    public class HtmlSecurityValidator
    {
        public static bool IsHtmlSafe(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return true;

            var sanitizer = new HtmlSanitizer();
            bool isMalicious = false;

            // لو المكتبة لقت أي تاج أو كلاس أو ستايل خطر وحاولت تمسحه، هنخلي المتغير بـ true
            sanitizer.RemovingTag += (s, e) => isMalicious = true;
            sanitizer.RemovingAttribute += (s, e) => isMalicious = true;
            sanitizer.RemovingStyle += (s, e) => isMalicious = true;
            sanitizer.RemovingAtRule += (s, e) => isMalicious = true;

            // نعمل Sanitize عشان الـ Events تشتغل لو فيه خطر
            sanitizer.Sanitize(html);

            // لو isMalicious فضلت false، يبقى الكود مفيهوش أي ثغرات
            return !isMalicious;
        }

    }
}
