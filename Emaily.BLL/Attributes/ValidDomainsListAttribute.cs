using System;
using System.ComponentModel.DataAnnotations;

namespace Emaily.BLL.Attributes
{
    public class ValidDomainsListAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            // التأكد من أن القيمة عبارة عن نص
            if (value is string domainsString)
            {
                // 2. غير مسموح أن يكون النص فارغاً أو مسافات فقط
                if (string.IsNullOrWhiteSpace(domainsString))
                {
                    return new ValidationResult("Restricted domains cannot be empty or whitespace only.");
                }

                // 3. تقسيم النص بالفاصلة (الكوما)
                var domains = domainsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var domain in domains)
                {
                    // لو المستخدم كتب دومين فارغ بين كومتين مثل (domain1.com, , domain2.com)
                    if (string.IsNullOrWhiteSpace(domain))
                    {
                        return new ValidationResult("The domains list contains an empty or invalid entry.");
                    }

                    // 4. التحقق من صحة صيغة الدومين
                    // ترجع Dns إذا كان دومين صحيح (مثل example.com أو localhost)
                    if (Uri.CheckHostName(domain) != UriHostNameType.Dns)
                    {
                        return new ValidationResult($"The domain '{domain}' is not a valid domain name.");
                    }
                }

                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid data type for domains list.");
        }
    }
}