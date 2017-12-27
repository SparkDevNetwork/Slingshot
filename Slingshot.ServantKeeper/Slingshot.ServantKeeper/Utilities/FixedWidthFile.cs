using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Models;

namespace Slingshot.ServantKeeper.Utilities
{
    class FixedWidthFile
    {
        public static Table Convert(string fileName)
        {
            var f = File.ReadAllBytes(fileName);
            return Convert(f);
        }

        public static Table Convert(byte[] f)
        {
            var Table = new Table();

            int location = 32;

            //Getting header information
            while (true)
            {
                if (f[location] == 13 && f[location + 1] == 32)
                {
                    break;
                }

                var take = 0;
                for (var i = 0; i < 10; i++)
                {
                    if (f[location + i] != 0)
                    {
                        take = i;
                    }
                    else
                    {
                        break;
                    }
                }

                var name = Encoding.GetEncoding(Encoding.Default.CodePage).GetString(f.Skip(location).Take(take + 1).ToArray());
                var startsAt = (f[location + 13] * 256) + f[location + 12];
                Table.ColumnNames.Add(name.ToLower());
                Table.ColumnStart.Add(startsAt + 1);
                location += 32;
            }
            var columnWidth = (f[11] * 256 + f[10]);

            //filling out the rows
            while (true)
            {
                if (location + columnWidth > f.Length)
                {
                    break;
                }
                var slice = f.Skip(location).Take(columnWidth).ToArray();
                var row = new List<string>();

                for (var i = 0; i < Table.ColumnStart.Count; i++)
                {
                    var startsAt = Table.ColumnStart[i];
                    var take = columnWidth - startsAt;
                    if (Table.ColumnStart.Count - 1 != i)
                    {
                        take = Table.ColumnStart[i + 1] - startsAt;
                    }
                    byte[] b = new byte[take + 1];
                    Array.Copy(slice, startsAt, b, 0, take);
                    var text = Encoding.GetEncoding(Encoding.Default.CodePage).GetString(b);
                    row.Add(text.Replace("\0", "").Trim());
                }
                Table.Data.Add(row);
                location += columnWidth;
            }
            return Table;
        }
    }
}
