﻿using System;
using System.Collections.Generic;
using System.Linq;
using ShapeCrawler.Models.Settings;
using A = DocumentFormat.OpenXml.Drawing;

namespace ShapeCrawler.Models.TableComponents
{
    /// <summary>
    /// Represents a table's row.
    /// </summary>
    public class RowEx
    {
        #region Fields

        private List<Cell> _cells;
        private readonly A.TableRow _sdkTblRow;
        private readonly IShapeContext _spContext;

        #endregion

        #region Properties

        /// <summary>
        /// Returns row's cells.
        /// </summary>
        /// TODO: use custom collection
        public IList<Cell> Cells {
            get
            {
                if (_cells == null)
                {
                    ParseCells();
                }

                return _cells;
            }
        }

        #endregion

        #region Constructors

        public RowEx(A.TableRow xmlRow, IShapeContext spContext)
        {
            _sdkTblRow = xmlRow ?? throw new ArgumentNullException(nameof(xmlRow));
            _spContext = spContext ?? throw new ArgumentNullException(nameof(spContext));
        }

        #endregion

        #region Private Methods

        private void ParseCells()
        {
            var xmlCells = _sdkTblRow.Elements<A.TableCell>();
            _cells = new List<Cell>(xmlCells.Count());
            foreach (var c in xmlCells)
            {
                _cells.Add(new Cell(c, _spContext));
            }
        }

        #endregion
    }
}
