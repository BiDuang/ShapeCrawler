﻿using DocumentFormat.OpenXml;
using ShapeCrawler.Shared;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler;

internal sealed record SlideSize
{
    private readonly P.SlideSize pSlideSize;

    internal SlideSize(P.SlideSize pSlideSize)
    {
        this.pSlideSize = pSlideSize;
    }

    internal int Width() => UnitConverter.HorizontalEmuToPixel(pSlideSize.Cx!.Value);

    internal int Height() => UnitConverter.HorizontalEmuToPixel(pSlideSize.Cx!.Value);

    internal void UpdateWidth(int pixels)
    {
        var emu = UnitConverter.HorizontalPixelToEmu(pixels);
        this.pSlideSize.Cx = new Int32Value((int)emu);
    }
    
    internal void UpdateHeight(int pixels)
    {
        var emu = UnitConverter.VerticalPixelToEmu(pixels);
        this.pSlideSize.Cy = new Int32Value((int)emu);
    }

}