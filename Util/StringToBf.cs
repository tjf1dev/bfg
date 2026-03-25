using System.Text;
static class StringToBf
{
    public static string GenerateBf(string input)
    {
        var sb = new StringBuilder();
        int current = 0;

        foreach (char c in input)
        {
            int target = (int)c;
            string candidate = GetShortest(current, target);
            sb.Append(candidate);
            current = target;
        }

        return sb.ToString();
    }

    static string GetShortest(int current, int target)
    {
        var candidates = new List<string>();

        int delta = target - current;
        candidates.Add((delta > 0 ? new string('+', delta) : new string('-', -delta)) + ".");

        for (int factor = 2; factor <= 16; factor++)
        {
            int multiple = (int)Math.Round((double)target / factor);
            if (multiple == 0) continue;

            int remainder = target - factor * multiple;

            var sb = new StringBuilder();

            sb.Append("[-]");

            sb.Append(new string('+', factor));
            sb.Append("[>");
            sb.Append(multiple > 0 ? new string('+', multiple) : new string('-', -multiple));
            sb.Append("<-]>");

            if (remainder > 0)
                sb.Append(new string('+', remainder));
            else if (remainder < 0)
                sb.Append(new string('-', -remainder));

            sb.Append('.');

            candidates.Add(sb.ToString());
        }

        return candidates.MinBy(s => s.Length)!;
    }
}