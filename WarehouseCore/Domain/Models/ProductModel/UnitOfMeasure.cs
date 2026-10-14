namespace Domain.Models
{
    public enum UnitOfMeasure
    {
        // --- Discrete Items ---
        Pcs,          // Pieces - most common for retail items
        Pack,         // Package
        Box,          // Box / Case
        Set,          // Set / Kit

        // --- Weight ---
        Kg,           // Kilogram
        G,            // Gram
        Lb,           // Pound

        // --- Volume & Liquids ---
        L,            // Liter
        Ml,           // Milliliter
        Gal,          // Gallon

        // --- Length & Area ---
        M,            // Meter
        Cm,           // Centimeter
        Sqm           // Square Meter
    }
}