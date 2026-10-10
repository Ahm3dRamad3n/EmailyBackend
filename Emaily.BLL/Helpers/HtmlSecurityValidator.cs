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
            sanitizer.AllowedAttributes.Add("role");
            bool isMalicious = false;

            // لو المكتبة لقت أي تاج أو كلاس أو ستايل خطر وحاولت تمسحه، هنخلي المتغير بـ true
            sanitizer.RemovingTag += (s, e) => isMalicious = true;
            sanitizer.RemovingAttribute += (s, e) => isMalicious = true;
            sanitizer.RemovingStyle += (s, e) => isMalicious = true;

            // نعمل Sanitize عشان الـ Events تشتغل لو فيه خطر
            sanitizer.Sanitize(html);

            // لو isMalicious فضلت false، يبقى الكود مفيهوش أي ثغرات
            return !isMalicious;
        }

        public static bool IsHtmlSafeIgnoreExpressions
            (string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return true;

            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedAttributes.Add("role");

            bool isMalicious = false;

            sanitizer.RemovingTag += (s, e) =>
            {
                if (e.Tag.TagName.Contains("{{") || e.Tag.TagName.Contains("}}"))
                    return;

                isMalicious = true;
            };

            sanitizer.RemovingAttribute += (s, e) =>
            {
                if (e.Attribute.Value.Contains("{{") && e.Attribute.Value.Contains("}}"))
                    return;

                isMalicious = true;
            };

            sanitizer.RemovingStyle += (s, e) =>
            {
                if (e.Style.Value.Contains("{{") && e.Style.Value.Contains("}}"))
                    return;

                isMalicious = true;
            };

            sanitizer.Sanitize(html);

            return !isMalicious;
        }
    }

}

