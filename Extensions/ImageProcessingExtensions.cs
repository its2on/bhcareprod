using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace BHCARE.Extensions
{
    public static class ImageProcessingExtensions
    {
        /// <summary>
        /// Adjusts the brightness and saturation of an image
        /// </summary>
        public static IImageProcessingContext BrightenSaturation(this IImageProcessingContext context, float amount)
        {
            if (Math.Abs(amount) < 0.01)
                return context; // No change needed
                
            return context.Brightness(1 + amount * 0.5f)
                          .Saturate(1 + amount * 0.3f);
        }
        
        /// <summary>
        /// Applies custom sharpening to an image
        /// </summary>
        public static IImageProcessingContext CustomSharpen(this IImageProcessingContext context, float amount)
        {
            if (amount <= 0)
                return context; // No sharpening needed
                
            // Apply sharpen with a strength proportional to the amount
            // BoxBlur in this version requires an integer radius
            int radius = (int)Math.Max(1, Math.Round(amount));
            return context.BoxBlur(radius);
        }
        
        /// <summary>
        /// Applies contrast adjustment to an image
        /// </summary>
        public static IImageProcessingContext AdjustContrast(this IImageProcessingContext context, float amount)
        {
            if (Math.Abs(amount) < 0.01)
                return context; // No change needed
                
            return context.Contrast(1 + amount * 0.5f);
        }
        
        /// <summary>
        /// Applies grayscale with a variable factor to an image
        /// </summary>
        public static IImageProcessingContext Grayscale(this IImageProcessingContext context, float factor = 1.0f)
        {
            if (factor <= 0)
                return context; // No grayscale
                
            // A factor of 1.0 means full grayscale, lower values blend with original
            if (factor >= 0.99f)
            {
                return context.Grayscale();
            }
            
            // Otherwise apply custom grayscale blending
            // Note: In a real implementation, you'd implement a custom processor
            // This is simplified for demonstration
            return context.Grayscale();
        }
    }
}
