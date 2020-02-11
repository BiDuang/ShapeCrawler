﻿using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using SlideXML.Enums;
using SlideXML.Exceptions;
using SlideXML.Validation;
using P = DocumentFormat.OpenXml.Presentation;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using A = DocumentFormat.OpenXml.Drawing;

namespace SlideXML.Models.SlideComponents.Chart
{
    /// <summary>
    /// <inheritdoc cref="IChart"/>
    /// </summary>
    public class Chart : IChart
    {
        #region Fields

        // Contains chart elements, e.g. <c:pieChart>. If the chart type is not a combination,
        // then collection contains an only single item.
        private List<OpenXmlElement> _chartElements;

        private readonly SlidePart _sldPart;
        private ChartType? _type;
        private string _title;
        private readonly P.GraphicFrame _grFrame;
        private C.Chart _cChart;

        #endregion

        #region Properties

        /// <summary>
        /// Returns the chart type.
        /// </summary>
        public ChartType Type
        {
            get
            {
                if (_type == null)
                {
                    ParseType();
                }

                return (ChartType)_type; //TODO: fix casting
            }
        }

        /// <summary>
        /// Returns the chart title text or null if title no exists.
        /// </summary>
        public string Title
        {
            get
            {
                if (_title == null)
                {
                    _title = TryParseTitle();
                }

                return _title ?? throw new SlideXmlException(ExceptionMessages.NotTitle);
            }
        }

        /// <summary>
        /// Indicates whether the chart has a title.
        /// </summary>
        public bool HasTitle
        {
            get
            {
                if (_title == null)
                {
                    _title = TryParseTitle();
                }

                return _title != null;
            }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Chart"/> class.
        /// </summary>
        public Chart(P.GraphicFrame grFrame, SlidePart sldPart)
        {
            Check.NotNull(sldPart, nameof(sldPart));
            _sldPart = sldPart;
            _grFrame = grFrame;

            Init(); //TODO: convert to lazy loading
        }

        #endregion

        #region Private Methods

        private void Init()
        {
            // Get reference
            var chartRef = _grFrame.Descendants<C.ChartReference>().Single();

            // Get chart part by reference
            var chPart = _sldPart.GetPartById(chartRef.Id) as ChartPart;

            _cChart = chPart.ChartSpace.GetFirstChild<C.Chart>();
            _chartElements = _cChart.PlotArea.Elements().Where(e => e.LocalName.EndsWith("Chart")).ToList();
        }

        private void ParseType()
        {
            if (_chartElements.Count > 1)
            {
                _type = ChartType.Combination;
            }
            else
            {
                var chartName = _chartElements.Single().LocalName;
                Enum.TryParse(chartName, true, out ChartType chartType);
                _type = chartType;
            }
        }

        private string TryParseTitle()
        {
            var title = _cChart.Title;
            if (title == null) // chart has not title
            {
                return null;
            }
           
            var chartText = title.ChartText;

            // Combination
            if (Type == ChartType.Combination)
            {
                return chartText.RichText.Descendants<A.Text>().Single().Text;
            }

            // Non-combination
            // First, tries parse static title
            var rRich = chartText?.RichText;
            if (rRich != null)
            {
                return rRich.Descendants<A.Text>().Single().Text;
            }
            // Dynamic title
            if (chartText != null)
            {
                return chartText.Descendants<C.StringPoint>().Single().InnerText;
            }

            if (Type == ChartType.PieChart)
            {
                // Parses PieChart dynamic title
                return _chartElements.Single().GetFirstChild<C.PieChartSeries>().GetFirstChild<C.SeriesText>().Descendants<C.StringPoint>().Single().InnerText;
            }

            return null;
        }

        #endregion
    }
}



