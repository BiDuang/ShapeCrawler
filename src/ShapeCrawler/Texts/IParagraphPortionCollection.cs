using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ShapeCrawler.AutoShapes;
using ShapeCrawler.Exceptions;
using ShapeCrawler.Services.Factories;
using ShapeCrawler.Shared;
using ShapeCrawler.Texts;
using A = DocumentFormat.OpenXml.Drawing;

// ReSharper disable once CheckNamespace
namespace ShapeCrawler;

/// <summary>
///     Represents collection of paragraph text portions.
/// </summary>
public interface IParagraphPortionCollection : IEnumerable<IParagraphPortion>
{
    /// <summary>
    ///     Gets the number of series items in the collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Gets the element at the specified index.
    /// </summary>
    IParagraphPortion this[int index] { get; }

    /// <summary>
    ///     Adds text portion.
    /// </summary>
    void AddText(string text);
    
    /// <summary>
    /// 	Adds Line Break.
    /// </summary>
    void AddLineBreak();

    /// <summary>
    ///     Removes portion item from collection.
    /// </summary>
    void Remove(IParagraphPortion removingPortion);

    /// <summary>
    ///     Removes portion items from collection.
    /// </summary>
    void Remove(IList<IParagraphPortion> portions);
}

internal sealed class SlideParagraphPortions : IParagraphPortionCollection
{
    private readonly ResetableLazy<List<IParagraphPortion>> portions;
    private readonly SlidePart sdkSlidePart;
    private readonly A.Paragraph aParagraph;

    internal SlideParagraphPortions(SlidePart sdkSlidePart, A.Paragraph aParagraph)
    {
        this.sdkSlidePart = sdkSlidePart;
        this.aParagraph = aParagraph;
        this.portions = new ResetableLazy<List<IParagraphPortion>>(this.ParsePortions);
    }
    
    public int Count => this.portions.Value.Count;

    public IParagraphPortion this[int index] => this.portions.Value[index];

    public void AddText(string text)
    {
        if (text.Contains(Environment.NewLine))
        {
            throw new SCException(
                $"Text can not contain New Line. Use {nameof(IParagraphPortionCollection.AddLineBreak)} to add Line Break.");
        }
        
        var lastARunOrABreak = this.aParagraph.LastOrDefault(p => p is A.Run or A.Break);

        var textPortions = this.portions.Value.OfType<TextParagraphPortion>();
        var lastPortion = textPortions.Any() ? textPortions.Last() : null;
        var aTextParent = lastPortion?.AText.Parent ?? new ARunBuilder().Build();

        AddText(ref lastARunOrABreak, aTextParent, text, this.aParagraph);

        this.portions.Reset();
    }

    public void AddLineBreak()
    {
        throw new System.NotImplementedException();
    }

    public void Remove(IParagraphPortion removingPortion)
    {
        removingPortion.Remove();

        this.portions.Reset();
    }

    public void Remove(IList<IParagraphPortion> removingPortions)
    {
        foreach (var portion in removingPortions)
        {
            this.Remove(portion);
        }
    }

    public IEnumerator<IParagraphPortion> GetEnumerator()
    {
        return this.portions.Value.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
    
    internal void AddNewLine()
    {
        var lastARunOrABreak = this.aParagraph.Last();
        lastARunOrABreak.InsertAfterSelf(new A.Break());
    }

    private static void AddText(ref OpenXmlElement? lastElement, OpenXmlElement aTextParent, string text, A.Paragraph aParagraph)
    {
        var newARun = (A.Run)aTextParent.CloneNode(true);
        newARun.Text!.Text = text;
        if (lastElement == null)
        {
            var apPr = aParagraph.GetFirstChild<A.ParagraphProperties>();
            lastElement = apPr != null ? apPr.InsertAfterSelf(newARun) : aParagraph.InsertAt(newARun, 0);
        }
        else
        {
            lastElement = lastElement.InsertAfterSelf(newARun);
        }
    }

    private List<IParagraphPortion> ParsePortions()
    {
        var portions = new List<IParagraphPortion>();
        foreach (var paraChild in this.aParagraph.Elements())
        {
            switch (paraChild)
            {
                case A.Run aRun:
                    var runPortion = new TextParagraphPortion(this.sdkSlidePart, aRun); 
                    portions.Add(runPortion);
                    break;
                case A.Field aField:
                {
                    var fieldPortion = new SlideField(this.sdkSlidePart, aField);
                    portions.Add(fieldPortion);
                    break;
                }

                case A.Break aBreak:
                    var lineBreak = new ParagraphLineBreak(aBreak, () => this.portions.Reset());
                    portions.Add(lineBreak);
                    break;
            }
        }
        
        return portions;
    }
}