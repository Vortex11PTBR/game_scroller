using UnityEngine;

public static class NumberFormatter
{
    private const int THOUSAND = 1000;
    private const int MILLION = 1000000;
    private const int BILLION = 1000000000;

    public static string FormatCurrency(int amount, int decimalPlaces = 1)
    {
        if (amount >= BILLION)
        {
            return (amount / (float)BILLION).ToString($"F{decimalPlaces}") + "B";
        }

        if (amount >= MILLION)
        {
            return (amount / (float)MILLION).ToString($"F{decimalPlaces}") + "M";
        }

        if (amount >= THOUSAND)
        {
            return (amount / (float)THOUSAND).ToString($"F{decimalPlaces}") + "K";
        }

        return amount.ToString();
    }

    public static string FormatCurrency(long amount, int decimalPlaces = 1)
    {
        return FormatCurrency((int)Mathf.Clamp(amount, int.MinValue, int.MaxValue), decimalPlaces);
    }

    public static string FormatWithSeparator(int amount)
    {
        return amount.ToString("N0");  // Ex: 1,500,000
    }

    public static string FormatWithSeparator(long amount)
    {
        return amount.ToString("N0");
    }

    public static string FormatCompact(int amount, bool useSymbol = true)
    {
        if (amount < 0)
            return "-" + FormatCurrency(Mathf.Abs(amount), 0);

        if (!useSymbol)
        {
            if (amount >= BILLION)
                return (amount / (float)BILLION).ToString("F0");
            if (amount >= MILLION)
                return (amount / (float)MILLION).ToString("F0");
            if (amount >= THOUSAND)
                return (amount / (float)THOUSAND).ToString("F0");
        }

        return FormatCurrency(amount, 0);
    }

    public static string FormatPercent(float percent, int decimalPlaces = 1)
    {
        return percent.ToString($"F{decimalPlaces}") + "%";
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 60)
            return Mathf.RoundToInt(seconds) + "s";

        if (seconds < 3600)
        {
            int minutes = Mathf.RoundToInt(seconds / 60);
            return minutes + "m";
        }

        int hours = Mathf.RoundToInt(seconds / 3600);
        return hours + "h";
    }
}
