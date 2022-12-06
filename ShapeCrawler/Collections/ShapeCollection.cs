﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using OneOf;
using ShapeCrawler.Extensions;
using ShapeCrawler.Factories;
using ShapeCrawler.Media;
using ShapeCrawler.Placeholders;
using ShapeCrawler.Shapes;
using ShapeCrawler.SlideMasters;
using ShapeCrawler.Statics;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using P14 = DocumentFormat.OpenXml.Office2010.PowerPoint;

namespace ShapeCrawler.Collections;

internal class ShapeCollection : LibraryCollection<IShape>, IShapeCollection
{
    private readonly P.ShapeTree shapeTree;
    private readonly OneOf<SCSlide, SCSlideLayout, SCSlideMaster> slideObject;

    private ShapeCollection(
        List<IShape> shapes, 
        P.ShapeTree shapeTree, 
        OneOf<SCSlide, SCSlideLayout, SCSlideMaster> slideObject)
        : base(shapes)
    {
        this.slideObject = slideObject;
        this.shapeTree = shapeTree;
    }

    public IAudioShape AddAudio(int xPixels, int yPixels, Stream mp3Stream)
    {
        long xEmu = UnitConverter.HorizontalPixelToEmu(xPixels);
        long yEmu = UnitConverter.VerticalPixelToEmu(yPixels);

        var slideBase = this.slideObject.Match(slide => slide as SlideObject, layout => layout, master => master);
        var mediaDataPart =
            slideBase.PresentationInternal.SDKPresentationInternal.CreateMediaDataPart("audio/mpeg", ".mp3");

        mp3Stream.Position = 0;
        mediaDataPart.FeedData(mp3Stream);
        string imgPartRId = $"rId{Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 5)}";
        var slidePart = slideBase.TypedOpenXmlPart as SlidePart;
        var imagePart = slidePart!.AddNewPart<ImagePart>("image/png", imgPartRId);
        var imgStream = Assembly.GetExecutingAssembly().GetStream("audio-image.png");
        imgStream.Position = 0;
        imagePart.FeedData(imgStream);

        var audioRr = slidePart.AddAudioReferenceRelationship(mediaDataPart);
        var mediaRr = slidePart.AddMediaReferenceRelationship(mediaDataPart);

        P.Picture picture1 = new();

        P.NonVisualPictureProperties nonVisualPictureProperties1 = new();

        uint shapeId = (uint)this.CollectionItems.Max(sp => sp.Id) + 1;
        P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new() { Id = shapeId, Name = $"Audio{shapeId}" };
        A.HyperlinkOnClick hyperlinkOnClick1 = new A.HyperlinkOnClick() { Id = "", Action = "ppaction://media" };

        A.NonVisualDrawingPropertiesExtensionList nonVisualDrawingPropertiesExtensionList1 = new();

        A.NonVisualDrawingPropertiesExtension nonVisualDrawingPropertiesExtension1 =
            new() { Uri = "{FF2B5EF4-FFF2-40B4-BE49-F238E27FC236}" };

        OpenXmlUnknownElement openXmlUnknownElement1 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement(
            "<a16:creationId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" id=\"{2FF36D28-5328-4DA3-BF85-A2B65D7EE127}\" />");

        nonVisualDrawingPropertiesExtension1.Append(openXmlUnknownElement1);

        nonVisualDrawingPropertiesExtensionList1.Append(nonVisualDrawingPropertiesExtension1);

        nonVisualDrawingProperties2.Append(hyperlinkOnClick1);
        nonVisualDrawingProperties2.Append(nonVisualDrawingPropertiesExtensionList1);

        P.NonVisualPictureDrawingProperties nonVisualPictureDrawingProperties1 = new();
        A.PictureLocks pictureLocks1 = new A.PictureLocks() { NoChangeAspect = true };

        nonVisualPictureDrawingProperties1.Append(pictureLocks1);

        P.ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new();
        A.AudioFromFile audioFromFile1 = new A.AudioFromFile() { Link = audioRr.Id };

        P.ApplicationNonVisualDrawingPropertiesExtensionList
            applicationNonVisualDrawingPropertiesExtensionList1 = new();

        P.ApplicationNonVisualDrawingPropertiesExtension applicationNonVisualDrawingPropertiesExtension1 =
            new() { Uri = "{DAA4B4D4-6D71-4841-9C94-3DE7FCFB9230}" };

        P14.Media media1 = new P14.Media() { Embed = mediaRr.Id };
        media1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

        applicationNonVisualDrawingPropertiesExtension1.Append(media1);

        applicationNonVisualDrawingPropertiesExtensionList1.Append(applicationNonVisualDrawingPropertiesExtension1);

        applicationNonVisualDrawingProperties2.Append(audioFromFile1);
        applicationNonVisualDrawingProperties2.Append(applicationNonVisualDrawingPropertiesExtensionList1);

        nonVisualPictureProperties1.Append(nonVisualDrawingProperties2);
        nonVisualPictureProperties1.Append(nonVisualPictureDrawingProperties1);
        nonVisualPictureProperties1.Append(applicationNonVisualDrawingProperties2);

        P.BlipFill blipFill1 = new();
        A.Blip blip1 = new() { Embed = imgPartRId };

        A.Stretch stretch1 = new();
        A.FillRectangle fillRectangle1 = new();

        stretch1.Append(fillRectangle1);

        blipFill1.Append(blip1);
        blipFill1.Append(stretch1);

        P.ShapeProperties shapeProperties1 = new();

        A.Transform2D transform2D1 = new();
        A.Offset offset2 = new() { X = xEmu, Y = yEmu };
        A.Extents extents2 = new() { Cx = 609600L, Cy = 609600L };

        transform2D1.Append(offset2);
        transform2D1.Append(extents2);

        A.PresetGeometry presetGeometry1 = new() { Preset = A.ShapeTypeValues.Rectangle };
        A.AdjustValueList adjustValueList1 = new();

        presetGeometry1.Append(adjustValueList1);

        shapeProperties1.Append(transform2D1);
        shapeProperties1.Append(presetGeometry1);

        picture1.Append(nonVisualPictureProperties1);
        picture1.Append(blipFill1);
        picture1.Append(shapeProperties1);

        this.shapeTree.Append(picture1);

        P14.CreationId creationId1 = new() { Val = (UInt32Value)3972997422U };
        creationId1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

        return new AudioShape(this.shapeTree, this.slideObject);
    }

    public IVideoShape AddVideo(int x, int y, Stream stream)
    {
        long xEmu = UnitConverter.HorizontalPixelToEmu(x);
        long yEmu = UnitConverter.VerticalPixelToEmu(y);

        var slideBase = this.slideObject.Match(slide => slide as SlideObject, layout => layout, master => master);
        MediaDataPart mediaDataPart =
            slideBase.PresentationInternal.SDKPresentationInternal.CreateMediaDataPart("video/mp4", ".mp4");

        stream.Position = 0;
        mediaDataPart.FeedData(stream);
        string imgPartRId = $"rId{Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 5)}";
        var slidePart = slideBase.TypedOpenXmlPart as SlidePart;
        var imagePart = slidePart!.AddNewPart<ImagePart>("image/png", imgPartRId);
        var imageStream = Assembly.GetExecutingAssembly().GetStream("video-image.bmp");
        imagePart.FeedData(imageStream);

        var videoRr = slidePart.AddVideoReferenceRelationship(mediaDataPart);
        var mediaRr = slidePart.AddMediaReferenceRelationship(mediaDataPart);

        P.Picture picture1 = new();

        P.NonVisualPictureProperties nonVisualPictureProperties1 = new();

        uint shapeId = (uint)this.CollectionItems.Max(sp => sp.Id) + 1;
        P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new() { Id = shapeId, Name = $"Video{shapeId}" };
        A.HyperlinkOnClick hyperlinkOnClick1 = new A.HyperlinkOnClick() { Id = "", Action = "ppaction://media" };

        A.NonVisualDrawingPropertiesExtensionList nonVisualDrawingPropertiesExtensionList1 = new();

        A.NonVisualDrawingPropertiesExtension nonVisualDrawingPropertiesExtension1 =
            new() { Uri = "{FF2B5EF4-FFF2-40B4-BE49-F238E27FC236}" };

        OpenXmlUnknownElement openXmlUnknownElement1 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement(
            "<a16:creationId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" id=\"{2FF36D28-5328-4DA3-BF85-A2B65D7EE127}\" />");

        nonVisualDrawingPropertiesExtension1.Append(openXmlUnknownElement1);

        nonVisualDrawingPropertiesExtensionList1.Append(nonVisualDrawingPropertiesExtension1);

        nonVisualDrawingProperties2.Append(hyperlinkOnClick1);
        nonVisualDrawingProperties2.Append(nonVisualDrawingPropertiesExtensionList1);

        P.NonVisualPictureDrawingProperties nonVisualPictureDrawingProperties1 = new();
        A.PictureLocks pictureLocks1 = new A.PictureLocks() { NoChangeAspect = true };

        nonVisualPictureDrawingProperties1.Append(pictureLocks1);

        P.ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new();
        A.VideoFromFile videoFromFile1 = new A.VideoFromFile() { Link = videoRr.Id };

        P.ApplicationNonVisualDrawingPropertiesExtensionList
            applicationNonVisualDrawingPropertiesExtensionList1 = new();

        P.ApplicationNonVisualDrawingPropertiesExtension applicationNonVisualDrawingPropertiesExtension1 =
            new() { Uri = "{DAA4B4D4-6D71-4841-9C94-3DE7FCFB9230}" };

        P14.Media media1 = new P14.Media() { Embed = mediaRr.Id };
        media1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

        applicationNonVisualDrawingPropertiesExtension1.Append(media1);

        applicationNonVisualDrawingPropertiesExtensionList1.Append(applicationNonVisualDrawingPropertiesExtension1);

        applicationNonVisualDrawingProperties2.Append(videoFromFile1);
        applicationNonVisualDrawingProperties2.Append(applicationNonVisualDrawingPropertiesExtensionList1);

        nonVisualPictureProperties1.Append(nonVisualDrawingProperties2);
        nonVisualPictureProperties1.Append(nonVisualPictureDrawingProperties1);
        nonVisualPictureProperties1.Append(applicationNonVisualDrawingProperties2);

        P.BlipFill blipFill1 = new ();
        A.Blip blip1 = new() { Embed = imgPartRId };

        A.Stretch stretch1 = new ();
        A.FillRectangle fillRectangle1 = new ();

        stretch1.Append(fillRectangle1);

        blipFill1.Append(blip1);
        blipFill1.Append(stretch1);

        P.ShapeProperties shapeProperties1 = new();

        A.Transform2D transform2D1 = new();
        A.Offset offset2 = new() { X = xEmu, Y = yEmu };
        A.Extents extents2 = new() { Cx = 609600L, Cy = 609600L };

        transform2D1.Append(offset2);
        transform2D1.Append(extents2);

        A.PresetGeometry presetGeometry1 = new() { Preset = A.ShapeTypeValues.Rectangle };
        A.AdjustValueList adjustValueList1 = new();

        presetGeometry1.Append(adjustValueList1);

        shapeProperties1.Append(transform2D1);
        shapeProperties1.Append(presetGeometry1);

        picture1.Append(nonVisualPictureProperties1);
        picture1.Append(blipFill1);
        picture1.Append(shapeProperties1);

        this.shapeTree.Append(picture1);

        P14.CreationId creationId1 = new() { Val = (UInt32Value)3972997422U };
        creationId1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

        return new VideoShape(this.slideObject, this.shapeTree);
    }

    public IAutoShape AddAutoShape(SCAutoShapeType type, int x, int y, int width, int height)
    {
        var idAndName = this.GenerateIdAndName();

        var adjustValueList = new A.AdjustValueList();
        var presetGeometry = new A.PresetGeometry(adjustValueList) { Preset = A.ShapeTypeValues.Rectangle };
        var shapeProperties = new P.ShapeProperties();
        var xEmu = UnitConverter.HorizontalPixelToEmu(x);
        var yEmu = UnitConverter.VerticalPixelToEmu(y);
        var widthEmu = UnitConverter.HorizontalPixelToEmu(width);
        var heightEmu = UnitConverter.VerticalPixelToEmu(height);
        shapeProperties.AddAXfrm(xEmu, yEmu, widthEmu, heightEmu);
        shapeProperties.Append(presetGeometry);
        
        var newPShape = new P.Shape(
            new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = (uint)idAndName.Item1, Name = idAndName.Item2 },
            new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties()),
            shapeProperties,
            new P.TextBody(
            new A.BodyProperties(),
            new A.ListStyle(),
            new A.Paragraph(new A.EndParagraphRunProperties { Language = "en-US" })));

        this.shapeTree.Append(newPShape);
        
        var autoShape = new SlideAutoShape(newPShape, this.slideObject, null);

        return autoShape;
    }

    public ITable AddTable(int xPx, int yPx, int columns, int rows)
    {
        var shapeName = "Table 1";
        var xEmu = UnitConverter.HorizontalPixelToEmu(xPx);
        var yEmu = UnitConverter.VerticalPixelToEmu(yPx);
        var widthEmu = 8128000L;
        var heightEmu = 370840L;

        var graphicFrame = new GraphicFrame();
        var nonVisualGraphicFrameProperties = new NonVisualGraphicFrameProperties();
        var nonVisualDrawingProperties = new NonVisualDrawingProperties { Id = (UInt32Value)2U, Name = shapeName };
        var nonVisualDrawingPropertiesExtensionList = new A.NonVisualDrawingPropertiesExtensionList();
        var nonVisualDrawingPropertiesExtension = new A.NonVisualDrawingPropertiesExtension { Uri = "{FF2B5EF4-FFF2-40B4-BE49-F238E27FC236}" };
        var openXmlUnknownElement = OpenXmlUnknownElement.CreateOpenXmlUnknownElement(
            "<a16:creationId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" id=\"{384FFE3A-A793-EEBE-E597-6BD0DE1E7683}\" />");
        nonVisualDrawingPropertiesExtension.Append(openXmlUnknownElement);
        nonVisualDrawingPropertiesExtensionList.Append(nonVisualDrawingPropertiesExtension);
        nonVisualDrawingProperties.Append(nonVisualDrawingPropertiesExtensionList);
        var nonVisualGraphicFrameDrawingProperties = new NonVisualGraphicFrameDrawingProperties();
        var graphicFrameLocks = new A.GraphicFrameLocks { NoGrouping = true };
        nonVisualGraphicFrameDrawingProperties.Append(graphicFrameLocks);
        var applicationNonVisualDrawingProperties = new ApplicationNonVisualDrawingProperties();
        var applicationNonVisualDrawingPropertiesExtensionList = new ApplicationNonVisualDrawingPropertiesExtensionList();
        var applicationNonVisualDrawingPropertiesExtension = new ApplicationNonVisualDrawingPropertiesExtension { Uri = "{D42A27DB-BD31-4B8C-83A1-F6EECF244321}" };
        var modificationId = new P14.ModificationId { Val = (UInt32Value)3410121172U };
        modificationId.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");
        applicationNonVisualDrawingPropertiesExtension.Append(modificationId);
        applicationNonVisualDrawingPropertiesExtensionList.Append(applicationNonVisualDrawingPropertiesExtension);
        applicationNonVisualDrawingProperties.Append(applicationNonVisualDrawingPropertiesExtensionList);
        nonVisualGraphicFrameProperties.Append(nonVisualDrawingProperties);
        nonVisualGraphicFrameProperties.Append(nonVisualGraphicFrameDrawingProperties);
        nonVisualGraphicFrameProperties.Append(applicationNonVisualDrawingProperties);

        var pTransform = new P.Transform();
        var offset = new A.Offset { X = xEmu, Y = yEmu };
        var extents = new A.Extents { Cx = widthEmu, Cy = heightEmu };
        pTransform.Append(offset);
        pTransform.Append(extents);

        var graphic = new A.Graphic();
        var graphicData = new A.GraphicData { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" };
        var table = new A.Table();

        var tableProperties = new A.TableProperties { FirstRow = true, BandRow = true };
        var tableStyleId = new A.TableStyleId { Text = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}" };
        tableProperties.Append(tableStyleId);

        var tableGrid = new A.TableGrid();
        
        var gridColumn1 = new A.GridColumn { Width = 4064000L };
        var extensionList = new A.ExtensionList();
        var extension = new A.Extension { Uri = "{9D8B030D-6E8A-4147-A177-3AD203B41FA5}" };
        var openXmlUnknownElement2 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement(
            "<a16:colId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"1617513997\" />");
        extension.Append(openXmlUnknownElement2);
        extensionList.Append(extension);
        gridColumn1.Append(extensionList);
        
        var gridColumn2 = new A.GridColumn { Width = 4064000L };
        var extensionList2 = new A.ExtensionList();
        var extension2 = new A.Extension { Uri = "{9D8B030D-6E8A-4147-A177-3AD203B41FA5}" };
        var openXmlUnknownElement3 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement(
            "<a16:colId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"2215697276\" />");
        extension2.Append(openXmlUnknownElement3);
        extensionList2.Append(extension2);
        gridColumn2.Append(extensionList2);

        tableGrid.Append(gridColumn1);
        tableGrid.Append(gridColumn2);

        var tableRow = new A.TableRow { Height = 370840L };
        
        var tableCell = new A.TableCell();
        var textBody1 = new A.TextBody();
        var bodyProperties1 = new A.BodyProperties();
        var listStyle1 = new A.ListStyle();
        var paragraph1 = new A.Paragraph();
        var endParagraphRunProperties1 = new A.EndParagraphRunProperties { Language = "en-US" };
        paragraph1.Append(endParagraphRunProperties1);
        textBody1.Append(bodyProperties1);
        textBody1.Append(listStyle1);
        textBody1.Append(paragraph1);
        var tableCellProperties1 = new A.TableCellProperties();
        tableCell.Append(textBody1);
        tableCell.Append(tableCellProperties1);

        var tableCell2 = new A.TableCell();
        var textBody2 = new A.TextBody();
        var bodyProperties2 = new A.BodyProperties();
        var listStyle2 = new A.ListStyle();
        var paragraph2 = new A.Paragraph();
        var endParagraphRunProperties2 = new A.EndParagraphRunProperties { Language = "en-US", Dirty = false };
        paragraph2.Append(endParagraphRunProperties2);
        textBody2.Append(bodyProperties2);
        textBody2.Append(listStyle2);
        textBody2.Append(paragraph2);
        var tableCellProperties2 = new A.TableCellProperties();
        tableCell2.Append(textBody2);
        tableCell2.Append(tableCellProperties2);

        var extensionList3 = new A.ExtensionList();
        var extension3 = new A.Extension { Uri = "{0D108BD9-81ED-4DB2-BD59-A6C34878D82A}" };
        var openXmlUnknownElement4 = OpenXmlUnknownElement.CreateOpenXmlUnknownElement(
            "<a16:rowId xmlns:a16=\"http://schemas.microsoft.com/office/drawing/2014/main\" val=\"1618530090\" />");
        extension3.Append(openXmlUnknownElement4);
        extensionList3.Append(extension3);

        tableRow.Append(tableCell);
        tableRow.Append(tableCell2);
        tableRow.Append(extensionList3);

        table.Append(tableProperties);
        table.Append(tableGrid);
        table.Append(tableRow);

        graphicData.Append(table);
        graphic.Append(graphicData);

        graphicFrame.Append(nonVisualGraphicFrameProperties);
        graphicFrame.Append(pTransform);
        graphicFrame.Append(graphic);

        this.shapeTree.Append(graphicFrame);
        var newTable = new SCTable(graphicFrame, this.slideObject, null);

        return newTable;
    }

    public T GetById<T>(int shapeId)
        where T : IShape
    {
           var shape = this.CollectionItems.First(shape => shape.Id == shapeId);
           return (T)shape;
    }
    
    public T GetByName<T>(string shapeName)
        where T : IShape
    {
        var shape = this.CollectionItems.First(shape => shape.Name == shapeName);
  
        return (T)shape;
    }

    public Shape? GetReferencedShapeOrNull(P.PlaceholderShape inputPph)
    {
        var phShapes = this.CollectionItems.Where(sp => sp.Placeholder != null).OfType<Shape>();
        var referencedShape = phShapes.FirstOrDefault(IsEqual);

        // https://answers.microsoft.com/en-us/msoffice/forum/all/placeholder-master/0d51dcec-f982-4098-b6b6-94785304607a?page=3
        if (referencedShape == null && inputPph.Index?.Value == 4294967295 && this.slideObject.IsT2)
        {
            var custom = phShapes.Select(sp =>
            {
                var placeholder = (Placeholder?)sp.Placeholder;
                return new
                {
                    shape = sp,
                    index = placeholder?.PPlaceholderShape.Index?.Value
                };
            });

            return custom.FirstOrDefault(x => x.index == 1)?.shape;
        }

        return referencedShape;

        bool IsEqual(Shape collectionShape)
        {
            var placeholder = (Placeholder)collectionShape.Placeholder!;
            var pPh = placeholder.PPlaceholderShape;

            if (inputPph.Index is not null && pPh.Index is not null &&
                inputPph.Index == pPh.Index)
            {
                return true;
            }

            if (inputPph.Type == null || pPh.Type == null)
            {
                return false;
            }

            if (inputPph.Type == P.PlaceholderValues.Body &&
                inputPph.Index is not null && pPh.Index is not null)
            {
                return inputPph.Index == pPh.Index;
            }

            var left = inputPph.Type;
            if (inputPph.Type == PlaceholderValues.CenteredTitle)
            {
                left = PlaceholderValues.Title;
            }

            var right = pPh.Type;
            if (pPh.Type == PlaceholderValues.CenteredTitle)
            {
                right = PlaceholderValues.Title;
            }

            return left.Equals(right);
        }
    }

    internal static ShapeCollection Create(
        OneOf<SlidePart, SlideLayoutPart, SlideMasterPart> oneOfSlidePart,
        OneOf<SCSlide, SCSlideLayout, SCSlideMaster> oneOfSlide)
    {
        var chartGrFrameHandler = new ChartGraphicFrameHandler();
        var tableGrFrameHandler = new TableGraphicFrameHandler();
        var oleGrFrameHandler = new OleGraphicFrameHandler();
        var autoShapeCreator = new AutoShapeCreator();
        var pictureHandler = new PictureHandler();

        autoShapeCreator.Successor = oleGrFrameHandler;
        oleGrFrameHandler.Successor = pictureHandler;
        pictureHandler.Successor = chartGrFrameHandler;
        chartGrFrameHandler.Successor = tableGrFrameHandler;

        var pShapeTree = oneOfSlidePart.Match(
            slidePart => slidePart.Slide.CommonSlideData!.ShapeTree!,
            layoutPart => layoutPart.SlideLayout.CommonSlideData!.ShapeTree!,
            masterPart => masterPart.SlideMaster.CommonSlideData!.ShapeTree!);
        var shapes = new List<IShape>(pShapeTree.Count());
        foreach (var childElementOfShapeTree in pShapeTree.OfType<OpenXmlCompositeElement>())
        {
            IShape? shape;
            if (childElementOfShapeTree is P.GroupShape pGroupShape)
            {
                shape = new SCGroupShape(pGroupShape, oneOfSlide, null);
            }
            else if (childElementOfShapeTree is P.ConnectionShape)
            {
                shape = new SCConnectionShape(childElementOfShapeTree, oneOfSlide);
            }
            else
            {
                shape = autoShapeCreator.Create(childElementOfShapeTree, oneOfSlide, null);
            }

            if (shape != null)
            {
                shapes.Add(shape);
            }
        }

        return new ShapeCollection(shapes, pShapeTree, oneOfSlide);
    }
    
    private (int, string) GenerateIdAndName()
    {
        var maxOrder = 0;
        var maxId = 0;
        foreach (var shape in this.CollectionItems)
        {
            if (shape.Id > maxId)
            {
                maxId = shape.Id;
            }

            var matchOrder = Regex.Match(shape.Name, "(?!AutoShape )\\d+");
            if (matchOrder.Success)
            {
                var order = int.Parse(matchOrder.Value);
                if (order > maxOrder)
                {
                    maxOrder = order;
                }
            }
        }

        var shapeId = maxId + 1;
        var shapeName = $"AutoShape {maxOrder + 1}";
        
        return (shapeId, shapeName);
    }
}