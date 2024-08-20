using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SquareGrid.Common.Utils
{
    public static class StringX
    {
        public static string? GenerateSlug(this string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            string slug = input.ToLowerInvariant();
            slug = slug.Normalize(NormalizationForm.FormD);
            slug = Regex.Replace(slug, @"\p{IsCombiningDiacriticalMarks}+", string.Empty);
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = slug.Trim('-');
            return slug;
        }
    }
}
