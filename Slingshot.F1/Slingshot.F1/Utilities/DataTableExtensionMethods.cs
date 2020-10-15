using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Slingshot.F1.Utilities
{
    public static class DataTableExtensionMethods
    {
        /// <summary>
        /// Copies an array of <see cref="DataRow"/>s to a <see cref="DataTable"/> but returns an
        /// empty table matching the schema of the <paramref name="sourceTable"/> if the array is
        /// empty.
        /// </summary>
        /// <param name="rows">The <see cref="DataRow"/> array.</param>
        /// <param name="sourceTable">The source <see cref="DataTable"/>.</param>
        /// <returns></returns>
        public static DataTable CopyToDataTable_Safe( this DataRow[] rows, DataTable sourceTable )
        {
            if ( rows.Any() )
            {
                return rows.CopyToDataTable();
            }

            return sourceTable.Clone();
        }

        /// <summary>
        /// Copies a <see cref="List{T}"/> of <see cref="DataRow"/>s to a <see cref="DataTable"/>
        /// but returns an empty table matching the schema of the <paramref name="sourceTable"/>
        /// if the list is empty.
        /// </summary>
        /// <param name="rows">The <see cref="DataRow"/> list.</param>
        /// <param name="sourceTable">The source <see cref="DataTable"/>.</param>
        /// <returns></returns>
        public static DataTable CopyToDataTable_Safe( this List<DataRow> rows, DataTable sourceTable )
        {
            if ( rows.Any() )
            {
                return rows.CopyToDataTable();
            }

            return sourceTable.Clone();
        }

        /// <summary>
        /// Copies a <see cref="IEnumerable{T}"/> of <see cref="DataRow"/>s to a
        /// <see cref="DataTable"/> /// but returns an empty table matching the schema of the
        /// <paramref name="sourceTable"/> /// if the collection is empty.
        /// </summary>
        /// <param name="rows">The <see cref="DataRow"/> collection.</param>
        /// <param name="sourceTable">The source <see cref="DataTable"/>.</param>
        /// <returns></returns>
        public static DataTable CopyToDataTable_Safe( this IEnumerable<DataRow> rows, DataTable sourceTable )
        {
            if ( rows.Any() )
            {
                return rows.CopyToDataTable();
            }

            return sourceTable.Clone();
        }
    }
}
