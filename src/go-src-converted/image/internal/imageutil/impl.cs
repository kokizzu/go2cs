// generated by "go run gen.go". DO NOT EDIT.

// package imageutil -- go2cs converted at 2020 August 29 10:09:59 UTC
// import "image/internal/imageutil" ==> using imageutil = go.image.@internal.imageutil_package
// Original source: C:\Go\src\image\internal\imageutil\impl.go
using image = go.image_package;
using static go.builtin;

namespace go {
namespace image {
namespace @internal
{
    public static partial class imageutil_package
    {
        // DrawYCbCr draws the YCbCr source image on the RGBA destination image with
        // r.Min in dst aligned with sp in src. It reports whether the draw was
        // successful. If it returns false, no dst pixels were changed.
        //
        // This function assumes that r is entirely within dst's bounds and the
        // translation of r from dst coordinate space to src coordinate space is
        // entirely within src's bounds.
        public static bool DrawYCbCr(ref image.RGBA dst, image.Rectangle r, ref image.YCbCr src, image.Point sp)
        { 
            // This function exists in the image/internal/imageutil package because it
            // is needed by both the image/draw and image/jpeg packages, but it doesn't
            // seem right for one of those two to depend on the other.
            //
            // Another option is to have this code be exported in the image package,
            // but we'd need to make sure we're totally happy with the API (for the
            // rest of Go 1 compatibility), and decide if we want to have a more
            // general purpose DrawToRGBA method for other image types. One possibility
            // is:
            //
            // func (src *YCbCr) CopyToRGBA(dst *RGBA, dr, sr Rectangle) (effectiveDr, effectiveSr Rectangle)
            //
            // in the spirit of the built-in copy function for 1-dimensional slices,
            // that also allowed a CopyFromRGBA method if needed.

            var x0 = (r.Min.X - dst.Rect.Min.X) * 4L;
            var x1 = (r.Max.X - dst.Rect.Min.X) * 4L;
            var y0 = r.Min.Y - dst.Rect.Min.Y;
            var y1 = r.Max.Y - dst.Rect.Min.Y;


            if (src.SubsampleRatio == image.YCbCrSubsampleRatio444) 
                {
                    var y__prev1 = y;
                    var sy__prev1 = sy;

                    var y = y0;
                    var sy = sp.Y;

                    while (y != y1)
                    {
                        var dpix = dst.Pix[y * dst.Stride..];
                        var yi = (sy - src.Rect.Min.Y) * src.YStride + (sp.X - src.Rect.Min.X);

                        var ci = (sy - src.Rect.Min.Y) * src.CStride + (sp.X - src.Rect.Min.X);
                        {
                            var x__prev2 = x;

                            var x = x0;

                            while (x != x1)
                            {
                                // This is an inline version of image/color/ycbcr.go's func YCbCrToRGB.
                                var yy1 = int32(src.Y[yi]) * 0x10101UL;
                                var cb1 = int32(src.Cb[ci]) - 128L;
                                var cr1 = int32(src.Cr[ci]) - 128L; 

                                // The bit twiddling below is equivalent to
                                //
                                // r := (yy1 + 91881*cr1) >> 16
                                // if r < 0 {
                                //     r = 0
                                // } else if r > 0xff {
                                //     r = ^int32(0)
                                // }
                                //
                                // but uses fewer branches and is faster.
                                // Note that the uint8 type conversion in the return
                                // statement will convert ^int32(0) to 0xff.
                                // The code below to compute g and b uses a similar pattern.
                                var r = yy1 + 91881L * cr1;
                                if (uint32(r) & 0xff000000UL == 0L)
                                {
                                    r >>= 16L;
                                x = x + 4L;
                            yi = yi + 1L;
                            ci = ci + 1L;
                                }
                                else
                                {
                                    r = ~(r >> (int)(31L));
                        y = y + 1L;
                    sy = sy + 1L;
                                }
                                var g = yy1 - 22554L * cb1 - 46802L * cr1;
                                if (uint32(g) & 0xff000000UL == 0L)
                                {
                                    g >>= 16L;
                                }
                                else
                                {
                                    g = ~(g >> (int)(31L));
                                }
                                var b = yy1 + 116130L * cb1;
                                if (uint32(b) & 0xff000000UL == 0L)
                                {
                                    b >>= 16L;
                                }
                                else
                                {
                                    b = ~(b >> (int)(31L));
                                }
                                var rgba = dpix.slice(x, x + 4L, len(dpix));
                                rgba[0L] = uint8(r);
                                rgba[1L] = uint8(g);
                                rgba[2L] = uint8(b);
                                rgba[3L] = 255L;
                            }

                            x = x__prev2;
                        }
                    }

                    y = y__prev1;
                    sy = sy__prev1;
                }
            else if (src.SubsampleRatio == image.YCbCrSubsampleRatio422) 
                {
                    var y__prev1 = y;
                    var sy__prev1 = sy;

                    y = y0;
                    sy = sp.Y;

                    while (y != y1)
                    {
                        dpix = dst.Pix[y * dst.Stride..];
                        yi = (sy - src.Rect.Min.Y) * src.YStride + (sp.X - src.Rect.Min.X);

                        var ciBase = (sy - src.Rect.Min.Y) * src.CStride - src.Rect.Min.X / 2L;
                        {
                            var x__prev2 = x;
                            var sx__prev2 = sx;

                            x = x0;
                            var sx = sp.X;

                            while (x != x1)
                            {
                                ci = ciBase + sx / 2L; 

                                // This is an inline version of image/color/ycbcr.go's func YCbCrToRGB.
                                yy1 = int32(src.Y[yi]) * 0x10101UL;
                                cb1 = int32(src.Cb[ci]) - 128L;
                                cr1 = int32(src.Cr[ci]) - 128L; 

                                // The bit twiddling below is equivalent to
                                //
                                // r := (yy1 + 91881*cr1) >> 16
                                // if r < 0 {
                                //     r = 0
                                // } else if r > 0xff {
                                //     r = ^int32(0)
                                // }
                                //
                                // but uses fewer branches and is faster.
                                // Note that the uint8 type conversion in the return
                                // statement will convert ^int32(0) to 0xff.
                                // The code below to compute g and b uses a similar pattern.
                                r = yy1 + 91881L * cr1;
                                if (uint32(r) & 0xff000000UL == 0L)
                                {
                                    r >>= 16L;
                                x = x + 4L;
                            sx = sx + 1L;
                            yi = yi + 1L;
                                }
                                else
                                {
                                    r = ~(r >> (int)(31L));
                        y = y + 1L;
                    sy = sy + 1L;
                                }
                                g = yy1 - 22554L * cb1 - 46802L * cr1;
                                if (uint32(g) & 0xff000000UL == 0L)
                                {
                                    g >>= 16L;
                                }
                                else
                                {
                                    g = ~(g >> (int)(31L));
                                }
                                b = yy1 + 116130L * cb1;
                                if (uint32(b) & 0xff000000UL == 0L)
                                {
                                    b >>= 16L;
                                }
                                else
                                {
                                    b = ~(b >> (int)(31L));
                                }
                                rgba = dpix.slice(x, x + 4L, len(dpix));
                                rgba[0L] = uint8(r);
                                rgba[1L] = uint8(g);
                                rgba[2L] = uint8(b);
                                rgba[3L] = 255L;
                            }

                            x = x__prev2;
                            sx = sx__prev2;
                        }
                    }

                    y = y__prev1;
                    sy = sy__prev1;
                }
            else if (src.SubsampleRatio == image.YCbCrSubsampleRatio420) 
                {
                    var y__prev1 = y;
                    var sy__prev1 = sy;

                    y = y0;
                    sy = sp.Y;

                    while (y != y1)
                    {
                        dpix = dst.Pix[y * dst.Stride..];
                        yi = (sy - src.Rect.Min.Y) * src.YStride + (sp.X - src.Rect.Min.X);

                        ciBase = (sy / 2L - src.Rect.Min.Y / 2L) * src.CStride - src.Rect.Min.X / 2L;
                        {
                            var x__prev2 = x;
                            var sx__prev2 = sx;

                            x = x0;
                            sx = sp.X;

                            while (x != x1)
                            {
                                ci = ciBase + sx / 2L; 

                                // This is an inline version of image/color/ycbcr.go's func YCbCrToRGB.
                                yy1 = int32(src.Y[yi]) * 0x10101UL;
                                cb1 = int32(src.Cb[ci]) - 128L;
                                cr1 = int32(src.Cr[ci]) - 128L; 

                                // The bit twiddling below is equivalent to
                                //
                                // r := (yy1 + 91881*cr1) >> 16
                                // if r < 0 {
                                //     r = 0
                                // } else if r > 0xff {
                                //     r = ^int32(0)
                                // }
                                //
                                // but uses fewer branches and is faster.
                                // Note that the uint8 type conversion in the return
                                // statement will convert ^int32(0) to 0xff.
                                // The code below to compute g and b uses a similar pattern.
                                r = yy1 + 91881L * cr1;
                                if (uint32(r) & 0xff000000UL == 0L)
                                {
                                    r >>= 16L;
                                x = x + 4L;
                            sx = sx + 1L;
                            yi = yi + 1L;
                                }
                                else
                                {
                                    r = ~(r >> (int)(31L));
                        y = y + 1L;
                    sy = sy + 1L;
                                }
                                g = yy1 - 22554L * cb1 - 46802L * cr1;
                                if (uint32(g) & 0xff000000UL == 0L)
                                {
                                    g >>= 16L;
                                }
                                else
                                {
                                    g = ~(g >> (int)(31L));
                                }
                                b = yy1 + 116130L * cb1;
                                if (uint32(b) & 0xff000000UL == 0L)
                                {
                                    b >>= 16L;
                                }
                                else
                                {
                                    b = ~(b >> (int)(31L));
                                }
                                rgba = dpix.slice(x, x + 4L, len(dpix));
                                rgba[0L] = uint8(r);
                                rgba[1L] = uint8(g);
                                rgba[2L] = uint8(b);
                                rgba[3L] = 255L;
                            }

                            x = x__prev2;
                            sx = sx__prev2;
                        }
                    }

                    y = y__prev1;
                    sy = sy__prev1;
                }
            else if (src.SubsampleRatio == image.YCbCrSubsampleRatio440) 
                {
                    var y__prev1 = y;
                    var sy__prev1 = sy;

                    y = y0;
                    sy = sp.Y;

                    while (y != y1)
                    {
                        dpix = dst.Pix[y * dst.Stride..];
                        yi = (sy - src.Rect.Min.Y) * src.YStride + (sp.X - src.Rect.Min.X);

                        ci = (sy / 2L - src.Rect.Min.Y / 2L) * src.CStride + (sp.X - src.Rect.Min.X);
                        {
                            var x__prev2 = x;

                            x = x0;

                            while (x != x1)
                            {
                                // This is an inline version of image/color/ycbcr.go's func YCbCrToRGB.
                                yy1 = int32(src.Y[yi]) * 0x10101UL;
                                cb1 = int32(src.Cb[ci]) - 128L;
                                cr1 = int32(src.Cr[ci]) - 128L; 

                                // The bit twiddling below is equivalent to
                                //
                                // r := (yy1 + 91881*cr1) >> 16
                                // if r < 0 {
                                //     r = 0
                                // } else if r > 0xff {
                                //     r = ^int32(0)
                                // }
                                //
                                // but uses fewer branches and is faster.
                                // Note that the uint8 type conversion in the return
                                // statement will convert ^int32(0) to 0xff.
                                // The code below to compute g and b uses a similar pattern.
                                r = yy1 + 91881L * cr1;
                                if (uint32(r) & 0xff000000UL == 0L)
                                {
                                    r >>= 16L;
                                x = x + 4L;
                            yi = yi + 1L;
                            ci = ci + 1L;
                                }
                                else
                                {
                                    r = ~(r >> (int)(31L));
                        y = y + 1L;
                    sy = sy + 1L;
                                }
                                g = yy1 - 22554L * cb1 - 46802L * cr1;
                                if (uint32(g) & 0xff000000UL == 0L)
                                {
                                    g >>= 16L;
                                }
                                else
                                {
                                    g = ~(g >> (int)(31L));
                                }
                                b = yy1 + 116130L * cb1;
                                if (uint32(b) & 0xff000000UL == 0L)
                                {
                                    b >>= 16L;
                                }
                                else
                                {
                                    b = ~(b >> (int)(31L));
                                }
                                rgba = dpix.slice(x, x + 4L, len(dpix));
                                rgba[0L] = uint8(r);
                                rgba[1L] = uint8(g);
                                rgba[2L] = uint8(b);
                                rgba[3L] = 255L;
                            }

                            x = x__prev2;
                        }
                    }

                    y = y__prev1;
                    sy = sy__prev1;
                }
            else 
                return false;
                        return true;
        }
    }
}}}