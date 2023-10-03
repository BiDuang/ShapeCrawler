﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ShapeCrawler.Charts;
using ShapeCrawler.SlideShape;
using ShapeCrawler.Texts;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Chart = ShapeCrawler.Charts.Chart;
using Picture = ShapeCrawler.Drawing.Picture;

namespace ShapeCrawler.Shapes;

internal sealed class ShapeList : IShapeList
{
    private readonly TypedOpenXmlPart sdkTypedOpenXmlPart;

    internal ShapeList(TypedOpenXmlPart sdkTypedOpenXmlPart)
    {
        this.sdkTypedOpenXmlPart = sdkTypedOpenXmlPart;
    }

    public int Count => this.Shapes().Count;
    public IShape this[int index] => this.Shapes()[index];
    public T GetById<T>(int id) where T : IShape => (T)this.Shapes().First(shape => shape.Id == id);
    public T GetByName<T>(string name) where T : IShape => (T)this.GetByName(name);
    public IShape GetByName(string name) => this.Shapes().First(shape => shape.Name == name);
    public IEnumerator<IShape> GetEnumerator() => this.Shapes().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private List<IShape> Shapes()
    {
        var pShapeTree = this.sdkTypedOpenXmlPart switch
        {
            SlidePart sdkSlidePart => sdkSlidePart.Slide.CommonSlideData!.ShapeTree!,
            SlideLayoutPart sdkSlideLayoutPart => sdkSlideLayoutPart.SlideLayout.CommonSlideData!.ShapeTree!,
            _ => ((SlideMasterPart)this.sdkTypedOpenXmlPart).SlideMaster.CommonSlideData!.ShapeTree!
        };
        var shapeList = new List<IShape>(pShapeTree.Count());
        foreach (var pShapeTreeElement in pShapeTree.OfType<TypedOpenXmlCompositeElement>())
        {
            if (pShapeTreeElement is P.GroupShape pGroupShape)
            {
                var groupShape = new GroupShape(this.sdkTypedOpenXmlPart, pGroupShape);
                shapeList.Add(groupShape);
            }
            else if (pShapeTreeElement is P.ConnectionShape pConnectionShape)
            {
                var line = new SlideLine(this.sdkTypedOpenXmlPart, pConnectionShape);
                shapeList.Add(line);
            }
            else if (pShapeTreeElement is P.Shape pShape)
            {
                if (pShape.TextBody is not null)
                {
                    shapeList.Add(
                        new DuplicateableShape(
                            this.sdkTypedOpenXmlPart,
                            pShape,
                            new AutoShape(
                                this.sdkTypedOpenXmlPart,
                                pShape,
                                new TextFrame(
                                    this.sdkTypedOpenXmlPart, pShape.TextBody
                                )
                            )
                        )
                    );
                }
                else
                {
                    shapeList.Add(
                        new DuplicateableShape(
                            this.sdkTypedOpenXmlPart,
                            pShape,
                            new AutoShape(
                                this.sdkTypedOpenXmlPart,
                                pShape
                            )
                        )
                    );
                }
            }
            else if (pShapeTreeElement is P.GraphicFrame pGraphicFrame)
            {
                var aGraphicData = pShapeTreeElement.GetFirstChild<A.Graphic>()!.GetFirstChild<A.GraphicData>();
                if (aGraphicData!.Uri!.Value!.Equals("http://schemas.openxmlformats.org/presentationml/2006/ole",
                        StringComparison.Ordinal))
                {
                    var oleObject = new OLEObject(this.sdkTypedOpenXmlPart, pGraphicFrame);
                    shapeList.Add(oleObject);
                    continue;
                }

                var pPicture = pShapeTreeElement.Descendants<P.Picture>().FirstOrDefault();
                if (pPicture != null)
                {
                    var aBlip = pPicture.GetFirstChild<P.BlipFill>()?.Blip;
                    var blipEmbed = aBlip?.Embed;
                    if (blipEmbed is null)
                    {
                        continue;
                    }

                    var picture = new Picture(this.sdkTypedOpenXmlPart, pPicture, aBlip!);
                    shapeList.Add(picture);
                    continue;
                }

                if (this.IsChartPGraphicFrame(pShapeTreeElement))
                {
                    aGraphicData = pShapeTreeElement.GetFirstChild<A.Graphic>() !.GetFirstChild<A.GraphicData>() !;
                    var cChartRef = aGraphicData.GetFirstChild<C.ChartReference>() !;
                    var sdkChartPart = (ChartPart)this.sdkTypedOpenXmlPart.GetPartById(cChartRef.Id!);
                    var cPlotArea = sdkChartPart.ChartSpace.GetFirstChild<C.Chart>() !.PlotArea;
                    var cCharts = cPlotArea!.Where(e => e.LocalName.EndsWith("Chart", StringComparison.Ordinal));
                    pShapeTreeElement.GetFirstChild<A.Graphic>() !.GetFirstChild<A.GraphicData>() !
                        .GetFirstChild<C.ChartReference>();
                    pGraphicFrame = (P.GraphicFrame)pShapeTreeElement;
                    if (cCharts.Count() > 1)
                    {
                        // Combination chart
                        var combinationChart = new Chart(
                            this.sdkTypedOpenXmlPart,
                            sdkChartPart,
                            pGraphicFrame,
                            new Categories(sdkChartPart, cCharts)
                        );
                        shapeList.Add(combinationChart);
                        continue;
                    }

                    var chartType = cCharts.Single().LocalName;

                    if (chartType is "lineChart" or "barChart" or "pieChart")
                    {
                        var lineChart = new Chart(
                            this.sdkTypedOpenXmlPart,
                            sdkChartPart,
                            pGraphicFrame,
                            new Categories(sdkChartPart, cCharts)
                        );
                        shapeList.Add(lineChart);
                        continue;
                    }

                    if (chartType is "scatterChart" or "bubbleChart")
                    {
                        var scatterChart = new Chart(
                            this.sdkTypedOpenXmlPart,
                            sdkChartPart,
                            pGraphicFrame,
                            new NullCategories()
                        );
                        shapeList.Add(scatterChart);
                        continue;
                    }

                    var chart = new Chart(
                        this.sdkTypedOpenXmlPart,
                        sdkChartPart,
                        pGraphicFrame,
                        new Categories(sdkChartPart, cCharts)
                    );
                    shapeList.Add(chart);
                }
                else if (this.IsTablePGraphicFrame(pShapeTreeElement))
                {
                    var table = new Table(this.sdkTypedOpenXmlPart, pShapeTreeElement);
                    shapeList.Add(table);
                }
            }
            else if (pShapeTreeElement is P.Picture pPicture)
            {
                var element = pPicture.NonVisualPictureProperties!.ApplicationNonVisualDrawingProperties!.ChildElements
                    .FirstOrDefault();

                switch (element)
                {
                    case A.AudioFromFile:
                    {
                        var aAudioFile = pPicture.NonVisualPictureProperties.ApplicationNonVisualDrawingProperties
                            .GetFirstChild<A.AudioFromFile>();
                        if (aAudioFile is not null)
                        {
                            var mediaShape = new MediaShape(this.sdkTypedOpenXmlPart, pPicture);
                            shapeList.Add(mediaShape);
                        }

                        continue;
                    }
                    case A.VideoFromFile:
                    {
                        var mediaShape = new MediaShape(this.sdkTypedOpenXmlPart, pPicture);
                        shapeList.Add(mediaShape);
                        continue;
                    }
                }

                var aBlip = pPicture.GetFirstChild<P.BlipFill>()?.Blip;
                var blipEmbed = aBlip?.Embed;
                if (blipEmbed is null)
                {
                    continue;
                }

                var picture = new Picture(this.sdkTypedOpenXmlPart, pPicture, aBlip!);
                shapeList.Add(picture);
            }
        }

        return shapeList;
    }

    private bool IsTablePGraphicFrame(TypedOpenXmlCompositeElement pShapeTreeChild)
    {
        if (pShapeTreeChild is P.GraphicFrame pGraphicFrame)
        {
            var graphicData = pGraphicFrame.Graphic!.GraphicData!;
            if (graphicData.Uri!.Value!.Equals("http://schemas.openxmlformats.org/drawingml/2006/table",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsChartPGraphicFrame(TypedOpenXmlCompositeElement pShapeTreeChild)
    {
        if (pShapeTreeChild is P.GraphicFrame)
        {
            var aGraphicData = pShapeTreeChild.GetFirstChild<A.Graphic>() !.GetFirstChild<A.GraphicData>() !;
            if (aGraphicData.Uri!.Value!.Equals("http://schemas.openxmlformats.org/drawingml/2006/chart",
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}