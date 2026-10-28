namespace Zayun.Domain.ValueObjects;

/// <summary>
/// An image's size in pixels, with the maths the photo pipeline needs
/// (aspect ratio, orientation, resize-to-fit, centred crop).
/// </summary>
public sealed class ImageDimensions : ValueObject
{
    /// <summary>
    /// Upper bound per side. High enough for large screenshots and posters (~16,000 px);
    /// the upload's file-size limit is the real guard against huge files.
    /// </summary>
    public const int MaxSide = 20_000;

    private ImageDimensions(int width, int height)
    {
        Width = width;
        Height = height;
    }

    // Get-only: no "with" expression can create invalid dimensions.
    public int Width { get; }
    public int Height { get; }

    public static ImageDimensions Create(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaxSide);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaxSide);
        return new ImageDimensions(width, height);
    }

    /// <summary>Width ÷ height: 4:3 = 1.333, 16:9 = 1.777, square = 1.0.</summary>
    public double AspectRatio => Width / (double)Height;

    public long TotalPixels => (long)Width * Height;

    public bool IsLandscape => Width > Height;
    public bool IsPortrait => Height > Width;
    public bool IsSquare => Width == Height;

    /// <summary>True if this is at least as big as the minimum in both directions.</summary>
    public bool IsAtLeast(int minWidth, int minHeight) => Width >= minWidth && Height >= minHeight;

    /// <summary>True if the shape is ratioWidth:ratioHeight (e.g. 4:3), allowing 1% for rounding.</summary>
    public bool HasAspect(int ratioWidth, int ratioHeight) =>
        Math.Abs(AspectRatio - ratioWidth / (double)ratioHeight) < 0.01;

    /// <summary>
    /// Scales down (never up) to fit inside maxWidth × maxHeight, keeping the shape.
    /// Never returns a side of 0.
    /// </summary>
    public ImageDimensions ResizeToFit(int maxWidth, int maxHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight);

        if (Width <= maxWidth && Height <= maxHeight)
            return this;   // already fits

        var ratio = Math.Min((double)maxWidth / Width, (double)maxHeight / Height);
        return new ImageDimensions(
            Math.Max(1, (int)Math.Round(Width * ratio)),
            Math.Max(1, (int)Math.Round(Height * ratio)));
    }

    /// <summary>
    /// The largest centred area with the given shape (e.g. 4:3) that fits inside this image.
    /// Useful when a picture must have a fixed shape.
    /// </summary>
    public ImageDimensions CropToAspect(int ratioWidth, int ratioHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ratioWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ratioHeight);

        var target = ratioWidth / (double)ratioHeight;
        return AspectRatio > target
            ? new ImageDimensions(Math.Max(1, (int)Math.Round(Height * target)), Height)   // too wide → trim the sides
            : new ImageDimensions(Width, Math.Max(1, (int)Math.Round(Width / target)));    // too tall → trim top and bottom
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Width;
        yield return Height;
    }

    /// <summary>"1200x900".</summary>
    public override string ToString() => $"{Width}x{Height}";
}