using Chemistry;

namespace MassSpectrometry;

public static class ChemistryExtensions
{
    /// <summary>
    /// Calculates m/z value for a given mass assuming charge comes from losing or gaining protons
    /// </summary>
    public static double ToMz(this IHasMass objectWithMass, int charge, Polarity polarity)
    {
        if (polarity == Polarity.Positive && charge < 0)
            charge = -charge;
        if (polarity == Polarity.Negative && charge > 0)
            charge = -charge;

        return Chemistry.ClassExtensions.ToMz(objectWithMass.MonoisotopicMass, charge);
    }

    /// <summary>
    /// Calculates m/z value for a given mass assuming charge comes from losing or gaining protons
    /// </summary>
    public static double ToMz(this double mass, int charge, Polarity polarity)
    {
        if (polarity == Polarity.Positive && charge < 0)
            charge = -charge;
        if (polarity == Polarity.Negative && charge > 0)
            charge = -charge;

        return Chemistry.ClassExtensions.ToMz(mass, charge);
    }

    /// <summary>
    /// Calculates m/z value for a given mass assuming charge comes from losing or gaining protons
    /// </summary>
    public static float ToMz(this float mass, int charge, Polarity polarity)
    {
        if (polarity == Polarity.Positive && charge < 0)
            charge = -charge;
        if (polarity == Polarity.Negative && charge > 0)
            charge = -charge;

        return Chemistry.ClassExtensions.ToMz(mass, charge);
    }

    /// <summary>
    /// Determines the original mass from an m/z value, assuming charge comes from a proton
    /// </summary>
    public static double ToMass(this double massToChargeRatio, int charge, Polarity polarity)
    {
        if (polarity == Polarity.Positive && charge < 0)
            charge = -charge;
        if (polarity == Polarity.Negative && charge > 0)
            charge = -charge;

        return Chemistry.ClassExtensions.ToMass(massToChargeRatio, charge);
    }

    /// <summary>
    /// Determines the original mass from an m/z value, assuming charge comes from a proton
    /// </summary>
    public static double ToMass(this float massToChargeRatio, int charge, Polarity polarity)
    {
        if (polarity == Polarity.Positive && charge < 0)
            charge = -charge;
        if (polarity == Polarity.Negative && charge > 0)
            charge = -charge;

        return Chemistry.ClassExtensions.ToMass(massToChargeRatio, charge);
    }
}
